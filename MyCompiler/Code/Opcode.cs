﻿namespace MyCompiler.Code;

public enum Opcode : byte
{
    OpConstant = 0x00,
    OpArray,
    OpHash,
    OpIndex,

    OpAdd,
    OpSub,
    OpMul,
    OpDiv,
    
    OpPop,
    
    OpTrue,
    OpFalse,
    OpNull,
    
    OpEqual,
    OpNotEqual,
    OpGreaterThan,
    
    OpMinus,
    OpBang,

    OpJumpNotTruthy,
    OpJump,

    OpGetGlobal,
    OpSetGlobal,
    OpGetLocal,
    OpSetLocal,
    OpGetBuiltin,
    OpGetFree,

    OpClosure,
    OpCall,
    OpReturnValue,
    OpReturn
}
