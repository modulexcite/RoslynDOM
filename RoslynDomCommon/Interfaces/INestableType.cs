namespace RoslynDom.Common
{
    public interface INestableType : IDom, IStemMember
    {
        string OriginalName { get; }
    }
}