// Copyright (c) 2014-2017 Wolfgang Borgsmüller
// All rights reserved.
// 
// This software may be modified and distributed under the terms
// of the BSD license. See the License.txt file for details.

// Array of structs, cef_struct_t *array

using System.Diagnostics;

// inherit from CefStructPtrArrayType because the public layer is the same
public class CefStructArrayType : CefStructPtrArrayType {

    public CefStructArrayType(Parameter structArg, Parameter countArg)
        : base(new Parameter(structArg, new CefStructPtrPtrType(structArg.ParameterType.AsCefStructPtrType, "*")), countArg) {
        Debug.Assert(Struct.Category == StructCategory.Values);
    }

    public override bool IsOut {
        get { return CountArg.ParameterType.IsOut; }
    }

    public override string PInvokeSymbol {
        get { return StructPtr.PInvokeSymbol; }
    }

    public override string OriginalSymbol {
        get {
            return StructPtr.OriginalSymbol;
        }
    }

    public override string NativeSymbol {
        get { return StructPtr.NativeSymbol + "*"; }
    }

    public override string PInvokeCallParameter(string var) {
        return string.Format("{0}, out int {1}_nomem", base.PInvokeCallParameter(var), var);
    }

    public override string PInvokeCallbackParameter(string var) {
        return string.Format("IntPtr {0}, int {0}_structsize", var);
    }

    public override string NativeCallParameter(string var, bool isConst) {
        return string.Format("{0}, int* {1}_nomem", base.NativeCallParameter(var, isConst), var);
    }

    public override string NativeCallbackParameter(string var, bool isConst) {
        return string.Format("{0} {1}, int {1}_structsize", StructPtr.NativeSymbol, var);
    }

    public override string PublicUnwrapExpression(string var) {
        return string.Format("{0}, out {1}_nomem", base.PublicUnwrapExpression(var), var);
    }

    public override string NativeUnwrapExpression(string var) {
        return string.Format("{0}_tmp", var);
    }

    public override string NativeWrapExpression(string var) {
        return string.Format("{0}, (int)sizeof({1})", var, Struct.OriginalSymbol);
    }

    public override void EmitPublicPreCallStatements(CodeBuilder b, string var) {
        base.EmitPublicPreCallStatements(b, var);
        b.AppendLine("int {0}_nomem;", var);
    }

    public override void EmitPublicPostCallStatements(CodeBuilder b, string var) {
        base.EmitPublicPostCallStatements(b, var);
        b.BeginBlock("if({0}_nomem != 0)", var);
        b.AppendLine("throw new OutOfMemoryException();");
        b.EndBlock();
    }

    public override void EmitNativePreCallStatements(CodeBuilder b, string var) {
        b.AppendLine("{0} *{1}_tmp = ({0}*)malloc({2} * sizeof({0}));", Struct.OriginalSymbol, var, CountArg.VarName);
        b.BeginBlock("if({0}_tmp)", var);
        b.BeginBlock("for(size_t i = 0; i < {0}; ++i)", CountArg.VarName);
        b.AppendLine("{0}_tmp[i] = *{0}[i];", var);
        b.EndBlock();
        b.AppendLine("*{0}_nomem = 0;", var);
        b.BeginElse();
        b.AppendLine("{0} = 0;", CountArg.VarName);
        b.AppendLine("*{0}_nomem = 1;", var);
        b.EndBlock();
    }

    public override void EmitNativePostCallStatements(CodeBuilder b, string var) {
        b.AppendLine("if({0}_tmp) free({0}_tmp);", var);
    }

    public override void EmitPublicEventArgFields(CodeBuilder b, string var) {
        b.AppendLine("internal IntPtr m_{0};", var);
        b.AppendLine("internal int m_{0}_structsize;", var);
        b.AppendLine("internal {0} m_{1};", CountArg.ParameterType.PInvokeSymbol, CountArg.VarName);
        b.AppendLine("internal {0} m_{1}_managed;", PublicSymbol, var);
    }

    public override void EmitRemoteEventArgFields(CodeBuilder b, string var) {
        b.AppendLine("internal {0} m_{1}_managed;", PublicSymbol, var);
    }

    public override void EmitPublicEventFieldInitializers(CodeBuilder b, string var) {
        b.AppendLine("e.m_{0} = {0};", var);
        b.AppendLine("e.m_{0}_structsize = {0}_structsize;", var);
        b.AppendLine("e.m_{0} = {0};", CountArg.VarName);
    }

    public override void EmitPublicEventArgGetterStatements(CodeBuilder b, string var) {
        b.BeginIf("m_{0}_managed == null", var);
        b.AppendLine("m_{0}_managed = new {1}[({2})m_{3}];", var, Struct.ClassName, CountArg.ParameterType.PublicSymbol, CountArg.VarName);
        b.AppendLine("var currentPtr = m_{0};", var);
        b.BeginBlock("for({0} i = 0; i < ({0})m_{1}; ++i)", CountArg.ParameterType.PublicSymbol, CountArg.VarName);
        b.AppendLine("m_{0}_managed[i] = {1}.Wrap(currentPtr);", var, Struct.ClassName);
        b.AppendLine("currentPtr += m_{0}_structsize;", var);
        b.EndBlock();
        b.EndBlock();
        b.AppendLine("return m_{0}_managed;", var);
    }

    public override void EmitRemoteEventArgGetterStatements(CodeBuilder b, string var) {
        b.BeginIf("m_{0}_managed == null", var);
        b.AppendLine("m_{0}_managed = new {1}[({2})m_{3}];", var, Struct.RemoteClassName, CountArg.ParameterType.PublicSymbol, CountArg.VarName);
        b.AppendLine("var currentPtr = m_{0};", var);
        b.BeginBlock("for({0} i = 0; i < ({0})m_{1}; ++i)", CountArg.ParameterType.PublicSymbol, CountArg.VarName);
        b.AppendLine("m_{0}_managed[i] = {1}.Wrap(currentPtr);", var, Struct.ClassName);
        b.AppendLine("currentPtr += m_{0}_structsize;", var);
        b.EndBlock();
        b.EndBlock();
        b.AppendLine("return m_{0}_managed;", var);
    }

    public override void EmitPostPublicRaiseEventStatements(CodeBuilder b, string var) {
        b.BeginIf("e.m_{0}_managed != null", var);
        b.BeginFor("e.m_{0}_managed.Length", var);
        b.AppendLine("e.m_{0}_managed[i].Dispose();", var);
        b.EndBlock();
        b.EndBlock();
    }

    public override void EmitPostRemoteRaiseEventStatements(CodeBuilder b, string var) {
        b.BeginIf("e.m_{0}_managed != null", var);
        b.BeginFor("e.m_{0}_managed.Length", var);
        b.AppendLine("e.m_{0}_managed[i].Dispose();", var);
        b.EndBlock();
        b.EndBlock();
    }
}