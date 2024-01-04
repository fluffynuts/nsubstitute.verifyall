NSubstitute.VerifyAll
---

Aim: to provide the convenient `.VerifyAll()` method from Moq on NSubstitute
proxies so that:

- it's easier to move from Moq to NSubstitute
- it's more convenient to verify a bunch of calls which have been set up

Not supported, and may never be, unless there's demand:
- `.Verifiable()`
- `.Verify()`
These methods are used to verify only a portion of the mocked
service - if you're setting up a bunch of mocks and only testing
one or two, it's not much effort to verify those calls later. However,
if you're setting up a bunch of calls or migrating code, being able to
leave `.VerifyAll()` in place is quite convenient.

Usage
---

1. install
2. add `using NSubstitute.VerifyAll` to the affected file(s)
3. use `.VerifyAll()` as you would with Moq (see below)

Convenience
---

One of the very convenient bits of Moq is the `.VerifyAll()` extension,
which reduces noise in a test where you're going to set up a substitute/mock
service with one or more mocked methods, and then verify that those methods
have been called as expected:

```csharp
var service = new Mock<IService>();
service.Setup(x => x.Add(3, 4))
    .Returns(7);
var consumer = new Consumer(service.Object);
// ... some time later:
thing.VerifyAll();
```

compared with NSubstitute's:
```csharp
var service = Substitute.For<IService>();
service
```