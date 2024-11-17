namespace Streamon.Tests;

public class StreamPositionTests
{
    [Fact]
    public void DefaultPositionIsStart() => Assert.Equal(StreamPosition.Start, default);

    [Fact]
    public void PassArithmeticAdditionStartHasNoEffect()
    {
        Assert.Equal(StreamPosition.Start, StreamPosition.Start + StreamPosition.Start);

        Assert.Equal(StreamPosition.End, StreamPosition.Start + StreamPosition.End);
        Assert.Equal(StreamPosition.End, StreamPosition.End + StreamPosition.Start);
    }

    [Fact]
    public void PassArithmeticAdditionAnyHasNoEffect()
    {
        Assert.Equal(StreamPosition.Any, StreamPosition.Any + StreamPosition.Any);

        Assert.Equal(StreamPosition.End, StreamPosition.End + StreamPosition.Any);
        Assert.Equal(StreamPosition.End, StreamPosition.Any + StreamPosition.End);

        Assert.Equal(StreamPosition.Start, StreamPosition.Start + StreamPosition.Any);
        Assert.Equal(StreamPosition.Start, StreamPosition.Any + StreamPosition.Start);
    }

    [Fact]
    public void PassArithmeticAdditionNoOverflowAfterEnd()
    {
        Assert.Equal(StreamPosition.End, StreamPosition.End + StreamPosition.End);
    }

    [Fact]
    public void PassComparisonChecks()
    {
        Assert.NotEqual(StreamPosition.End, StreamPosition.Start);
        Assert.NotEqual(StreamPosition.Start, StreamPosition.End);

        Assert.Equal(StreamPosition.Any, StreamPosition.Any);

        Assert.Equal(StreamPosition.Start, StreamPosition.Any);
        Assert.Equal(StreamPosition.Any, StreamPosition.Start);

        Assert.Equal(StreamPosition.End, StreamPosition.Any);
        Assert.Equal(StreamPosition.Any, StreamPosition.End);
    }

    [Fact]
    public void PassComparisonChecksForGreaterThan()
    {
        Assert.True(StreamPosition.End > StreamPosition.Start);
        Assert.False(StreamPosition.Start > StreamPosition.End);

        Assert.False(StreamPosition.End > StreamPosition.Any);
        Assert.False(StreamPosition.Any > StreamPosition.End);

        Assert.False(StreamPosition.Any > StreamPosition.Start);
        Assert.False(StreamPosition.Start > StreamPosition.Any);

#pragma warning disable CS1718 // Comparison made to same variable
        Assert.False(StreamPosition.End > StreamPosition.End);
        Assert.False(StreamPosition.Start > StreamPosition.Start);
        Assert.False(StreamPosition.Any > StreamPosition.Any);
#pragma warning restore CS1718 // Comparison made to same variable
    }

    [Fact]
    public void PassComparisonChecksForGreaterOrEqualThan()
    {
        Assert.True(StreamPosition.End >= StreamPosition.Start);
        Assert.False(StreamPosition.Start >= StreamPosition.End);

        Assert.True(StreamPosition.End >= StreamPosition.Any);
        Assert.True(StreamPosition.Any >= StreamPosition.End);

        Assert.True(StreamPosition.Any >= StreamPosition.Start);
        Assert.True(StreamPosition.Start >= StreamPosition.Any);

#pragma warning disable CS1718 // Comparison made to same variable
        Assert.True(StreamPosition.End >= StreamPosition.End);
        Assert.True(StreamPosition.Start >= StreamPosition.Start);
        Assert.True(StreamPosition.Any >= StreamPosition.Any);
#pragma warning restore CS1718 // Comparison made to same variable
    }

    [Fact]
    public void PassComparisonChecksForLessThan()
    {
        Assert.False(StreamPosition.End < StreamPosition.Start);
        Assert.True(StreamPosition.Start < StreamPosition.End);

        Assert.False(StreamPosition.End < StreamPosition.Any);
        Assert.False(StreamPosition.Any < StreamPosition.End);

        Assert.False(StreamPosition.Any < StreamPosition.Start);
        Assert.False(StreamPosition.Start < StreamPosition.Any);

#pragma warning disable CS1718 // Comparison made to same variable
        Assert.False(StreamPosition.End < StreamPosition.End);
        Assert.False(StreamPosition.Start < StreamPosition.Start);
        Assert.False(StreamPosition.Any < StreamPosition.Any);
#pragma warning restore CS1718 // Comparison made to same variable
    }

    [Fact]
    public void PassComparisonChecksForLessOrEqualThan()
    {
        Assert.False(StreamPosition.End <= StreamPosition.Start);
        Assert.True(StreamPosition.Start <= StreamPosition.End);

        Assert.True(StreamPosition.End <= StreamPosition.Any);
        Assert.True(StreamPosition.Any <= StreamPosition.End);

        Assert.True(StreamPosition.Any <= StreamPosition.Start);
        Assert.True(StreamPosition.Start <= StreamPosition.Any);

#pragma warning disable CS1718 // Comparison made to same variable
        Assert.True(StreamPosition.End <= StreamPosition.End);
        Assert.True(StreamPosition.Start <= StreamPosition.Start);
        Assert.True(StreamPosition.Any <= StreamPosition.Any);
#pragma warning restore CS1718 // Comparison made to same variable
    }
}
