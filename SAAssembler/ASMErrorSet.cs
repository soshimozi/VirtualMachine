﻿namespace MacroAssembler
{
    public enum AsmErrors
    {
        AsmInvalidcode,
        AsmUndefinedlabel,
        AsmInvalidaddress,
        AsmUnlabelled,
        AsmHasaddress,
        AsmNoaddress,
        AsmExcessfields,
        AsmMismatched,
        AsmNonalpha,
        AsmBadlabel,
        AsmInvalidchar,
        AsmInvalidquote,
        AsmOverflow
    }

    //public class AsmErrorset : Set
    //{
    //    public AsmErrorset()
    //        : base((int)AsmErrors.AsmOverflow)
    //    {
            
    //    }
    //}
}
