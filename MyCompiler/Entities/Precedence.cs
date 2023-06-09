﻿namespace MyCompiler.Entities;

public enum Precedence
{
    Lowest = 0,
    Equals,      // ==
    LessGreater, // > or <
    Sum,         // +
    Product,     // *
    Prefix,      // -X or !X
    Call,        // myFunction(X)
    Index        // array[index]
}
