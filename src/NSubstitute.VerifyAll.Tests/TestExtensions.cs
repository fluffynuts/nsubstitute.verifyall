namespace NSubstitute.VerifyAll.Tests;

public class Tests
{
    [Test]
    public void ShouldPassWith_MethodWithOneConfigurationAndOneCall()
    {
        // Arrange
        var sut = Substitute.For<ICalculator>();
        sut.Add(1, 2).Returns(3);
        // Act
        var result = sut.Add(1, 2);
        // Assert
        Expect(result)
            .To.Equal(3);
        sut.VerifyAll();
    }

    [Test]
    public void ShouldFailWith_MethodWithTwoConfigurationsAndOneCall()
    {
        // Arrange
        var sut = Substitute.For<ICalculator>();
        sut.Add(1, 2).Returns(3);
        sut.Add(2, 3).Returns(5);
        // Act
        var result = sut.Add(1, 2);
        // Assert
        Expect(result)
            .To.Equal(3);
        Expect(() => sut.VerifyAll())
            .To.Throw<VerifyCallsException>();
    }

    [Test]
    public void ShouldFailWith_MethodWithTwoConfigurationsOnTwoMethodsAndOneCall()
    {
        // Arrange
        var sut = Substitute.For<ICalculator>();
        sut.Add(1, 2).Returns(3);
        sut.Multiply(2, 3).Returns(6);
        // Act
        var result = sut.Add(1, 2);
        // Assert
        Expect(result)
            .To.Equal(3);
        Expect(() => sut.VerifyAll())
            .To.Throw<VerifyCallsException>();
    }

    [TestFixture]
    public class WhenSpecifyingMaxCalls
    {
        [Test]
        public void ShouldPassWhenOneCallOneInvocationOneExpected()
        {
            // Arrange
            var calculator = Substitute.For<ICalculator>();
            calculator.Add(Arg.Any<int>(), Arg.Any<int>())
                .Returns(ci => (int) ci.Args()[0] + (int) ci.Args()[1]);
            // Act
            calculator.Add(1, 2);
            // Assert
            Expect(() => calculator.VerifyAll())
                .Not.To.Throw();
            Expect(() => calculator.VerifyAll(1))
                .Not.To.Throw();
            Expect(() => calculator.VerifyAll(2))
                .To.Throw();
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void ShouldThrowForCallCountLessThanOne(
            int callCount
        )
        {
            // Arrange
            var calculator = Substitute.For<ICalculator>();
            calculator.Add(Arg.Any<int>(), Arg.Any<int>())
                .Returns(ci => (int) ci.Args()[0] + (int) ci.Args()[1]);
            // Act
            calculator.Add(1, 2);
            // Assert
            Expect(() => calculator.VerifyAll(callCount))
                .To.Throw<ArgumentException>()
                .For("maxCallsPerInvocation");
        }
    }

    public interface ICalculator
    {
        int Add(int a, int b);
        int Multiply(int a, int b);
    }

    public interface IService
    {
        public bool Store(IPerson person);
        public int Moo(out string message);
    }

    public interface IPerson
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}