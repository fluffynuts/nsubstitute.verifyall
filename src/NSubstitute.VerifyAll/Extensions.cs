using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Imported.PeanutButter.Utils;
using NSubstitute.Core;
using NSubstitute.Core.Arguments;
using NSubstitute.Proxies.CastleDynamicProxy;

namespace NSubstitute.VerifyAll;

/// <summary>
/// Adds the required Extension, `.VerifyAll()` on any
/// reference type
/// </summary>
public static class Extension
{
    /// <summary>
    /// Verifies that all configured calls on an NSubstitute proxy
    /// have been called with the configured arguments, much like
    /// Moq's .VerifyAll()
    /// </summary>
    /// <parameter name="actual">The mocked service or entity to test</parameter>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static void VerifyAll<T>(
        this T actual
    ) where T : class
    {
        VerifyReceivedCalls(
            actual,
            null
        );
    }

    /// <summary>
    /// Verifies that all configured calls on an NSubstitute proxy
    /// have been called with the configured arguments, much like
    /// Moq's .VerifyAll()
    /// </summary>
    /// <parameter name="actual">The mocked service or entity to test</parameter>
    /// <parameter name="maxCallsPerInvocation">
    /// Specifies the maximum number of times each set-up mock should have been called,
    /// useful to ensure that services aren't being mistakenly called multiple times.
    /// </parameter>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static void VerifyAll<T>(
        this T actual,
        int maxCallsPerInvocation
    ) where T : class
    {
        if (maxCallsPerInvocation < 1)
        {
            throw new ArgumentException(
                $"{nameof(maxCallsPerInvocation)} cannot be < 1",
                nameof(maxCallsPerInvocation)
            );
        }

        VerifyReceivedCalls(
            actual,
            maxCallsPerInvocation
        );
    }

    private static void VerifyReceivedCalls<T>(
        T actual,
        int? maxCallsPerInvocation
    ) where T : class
    {
        var callSpecs = FindAllCallSpecificationsFor(actual);

        if (callSpecs.IsEmpty())
        {
            throw new VerifyCallsException(
                "Cannot verify substitute usage: no configured calls found"
            );
        }

        var allCalls = actual.ReceivedCalls().ToArray();
        var missedConfiguredCalls = FindMissedConfiguredCalls(
            callSpecs,
            allCalls,
            maxCallsPerInvocation
        );
        var unconfiguredCalls = FindUnconfiguredCalls(allCalls, callSpecs);

        var passed = missedConfiguredCalls.IsEmpty() &&
            unconfiguredCalls.IsEmpty();
        if (passed)
        {
            return;
        }

        throw new VerifyCallsException(
            @$"Expected to have only been used as configured:
{
    GenerateMessageFor(unconfiguredCalls, missedConfiguredCalls)
}"
        );
    }

    private static List<ICall> FindUnconfiguredCalls(
        ICall[] allCalls,
        List<CallSpec> callSpecs
    )
    {
        var unconfiguredCalls = new List<ICall>();
        foreach (var received in allCalls)
        {
            if (WasConfigured(received, callSpecs))
            {
                continue;
            }

            unconfiguredCalls.Add(received);
        }

        return unconfiguredCalls;
    }

    private static List<CallSpec> FindMissedConfiguredCalls(
        List<CallSpec> callSpecs,
        ICall[] allCalls,
        int? maxCalls
    )
    {
        var missedConfiguredCalls = new List<CallSpec>();
        foreach (var spec in callSpecs)
        {
            if (WasCalled(spec, allCalls, maxCalls))
            {
                continue;
            }

            missedConfiguredCalls.Add(spec);
        }

        return missedConfiguredCalls;
    }

    private static bool WasConfigured(ICall received, List<CallSpec> callSpecs)
    {
        var seek = received.GetMethodInfo();
        var configuredCalls = callSpecs.Where(
            c => c.MethodInfo == seek
        ).ToArray();
        if (configuredCalls.Length == 0)
        {
            return false;
        }

        var foundMatch = true;
        foreach (var configured in configuredCalls)
        {
            if (!ArgsMatch(configured, received))
            {
                foundMatch = false;
            }
        }

        return foundMatch;
    }

    private static bool WasCalled(
        CallSpec callSpec,
        ICall[] allCalls,
        int? maxCalls
    )
    {
        var receivedCalls = allCalls.Where(
            c => c.GetMethodInfo() == callSpec.MethodInfo
        ).ToArray();
        if (receivedCalls.Length == 0)
        {
            return false;
        }

        var matches = 0;
        foreach (var receivedCall in receivedCalls)
        {
            if (ArgsMatch(callSpec, receivedCall))
            {
                matches++;
            }
        }

        return maxCalls is null
            ? matches > 0
            : matches == maxCalls;
    }

    private static List<CallSpec> FindAllCallSpecificationsFor<T>(
        T actual
    ) where T : class
    {
        var interceptor = actual
                .FindInterceptors()
                .OfType<CastleForwardingInterceptor>()
                .FirstOrDefault()
            ?? throw new InvalidSubstituteException("finding interceptor");
        var callRouter = interceptor.ReadMember<CallRouter>("_callRouter");
        var state = callRouter.ReadMember<SubstituteState>("_substituteState");
        var callResults = state.ReadMember<CallResults>("CallResults");
        var configuredCalls = callResults.ReadMember<object>("_results");
        var callSpecs = new List<CallSpec>();
        var wrapper = new EnumerableWrapper<object>(configuredCalls);

        foreach (var item in wrapper)
        {
            callSpecs.Add(
                new CallSpec(item)
            );
        }

        return callSpecs;
    }

    private static string GenerateMessageFor(
        List<ICall> unconfiguredCalls,
        List<CallSpec> missedConfiguredCalls
    )
    {
        return
            string.Join(
                "\n",
                new[]
                    {
                        GenerateMessageFor(unconfiguredCalls),
                        GenerateMessageFor(missedConfiguredCalls)
                    }.Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray()
            );
    }

    private static string GenerateMessageFor(
        List<CallSpec> missed
    )
    {
        if (missed.IsEmpty())
        {
            return "";
        }

        var parts = new List<string>()
        {
            "The following calls were configured, but not received"
        };
        foreach (var spec in missed)
        {
            parts.Add(RenderSignature(spec.MethodInfo));
        }

        return parts.JoinWith("\n");
    }

    private static string GenerateMessageFor(
        List<ICall> unconfiguredCalls
    )
    {
        if (unconfiguredCalls.IsEmpty())
        {
            return null;
        }

        var parts = new List<string>()
        {
            "The following calls were received, but not configured"
        };
        foreach (var call in unconfiguredCalls)
        {
            parts.Add(RenderSignature(call.GetMethodInfo()));
        }

        return parts.JoinWith("\n");
    }

    private static string RenderSignature(MethodInfo mi)
    {
        var parameters = mi.GetParameters();
        var parameterString = parameters.Select(p => $"{p.ParameterType} {p.Name}")
            .JoinWith(", ");
        return $"  {mi.Name}({parameterString})";
    }

    private static bool ArgsMatch(
        CallSpec callSpec,
        ICall received
    )
    {
        var receivedArgs = received.GetArguments();
        if (receivedArgs.Length != callSpec.ArgumentSpecifications.Length)
        {
            return false;
        }

        foreach (var item in receivedArgs.Zip(callSpec.ArgumentSpecifications, Pair.Create))
        {
            var (receivedArg, specifiedArg) = item;
            if (!specifiedArg.IsSatisfiedBy(receivedArg))
            {
                return false;
            }
        }

        return true;
    }

    private abstract class Pair
    {
        public static Pair<T1, T2> Create<T1, T2>(T1 v1, T2 v2)
        {
            return new Pair<T1, T2>(v1, v2);
        }
    }

    private class Pair<T1, T2>(
        T1 left,
        T2 right
    ) : Pair
    {
        public T1 Left { get; } = left;
        public T2 Right { get; } = right;

        public void Deconstruct(
            out T1 left,
            out T2 right
        )
        {
            left = Left;
            right = Right;
        }
    }

    private class CallSpec
    {
        public MethodInfo MethodInfo { get; }
        public IArgumentSpecification[] ArgumentSpecifications { get; }

        public CallSpec(
            object resultForCallSpec
        )
        {
            var callSpec = resultForCallSpec.ReadMember<CallSpecification>(
                "_callSpecification"
            );
            MethodInfo = callSpec.GetMethodInfo();
            ArgumentSpecifications = callSpec.ReadMember<IArgumentSpecification[]>(
                "_argumentSpecifications"
            );
        }
    }

    private static TMember ReadMember<TMember>(
        this object host,
        string path
    ) where TMember : class
    {
        var result = host.GetOrDefault<TMember>(path);
        if (result is null)
        {
            throw new InvalidSubstituteException(
                $"Trying to read '{path}' as type '{typeof(TMember)}'"
            );
        }

        return result;
    }

    private class InvalidSubstituteException(string context)
        : Exception($"Provided object doesn't look like a Substitute (whilst {context})");

    private static IInterceptor[] FindInterceptors<T>(
        this T obj
    )
    {
        return obj.TryGet<IInterceptor[]>("__interceptors", out var result)
            ? result
            : Array.Empty<IInterceptor>();
    }
}