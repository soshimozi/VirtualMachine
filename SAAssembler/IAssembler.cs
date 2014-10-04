namespace MacroAssembler
{
    public interface IAssembler
    {
        void AssembleLine(SaUnpackedlines srceLine, out bool errors);
    }
}
