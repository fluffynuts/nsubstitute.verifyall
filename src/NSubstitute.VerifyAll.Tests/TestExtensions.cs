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