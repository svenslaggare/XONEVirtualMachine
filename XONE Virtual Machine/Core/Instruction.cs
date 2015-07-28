using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Core
{
	/// <summary>
	/// The op-codes for the instructions
	/// </summary>
	public enum OpCodes : byte
	{
        Pop,
        LoadInt,
        LoadFloat,
		AddInt,
		SubInt,
		MulInt,
		DivInt,
        AddFloat,
        SubFloat,
        MulFloat,
        DivFloat,
        Call,
        Ret,
        LoadArgument,
        LoadLocal,
        StoreLocal,
        Branch,
        BranchEqual,
        BranchNotEqual,
        BranchGreaterThan,
        BranchGreaterOrEqual,
        BranchLessThan,
        BranchLessOrEqual
	}

	/// <summary>
	/// Represents an instruction
	/// </summary>
	public struct Instruction
	{
        private readonly string stringRepresentation;

        /// <summary>
        /// Returns the op.code
        /// </summary>
		public OpCodes OpCode { get; }

        /// <summary>
        /// Returns the int value
        /// </summary>
        public int IntValue { get; }

        /// <summary>
        /// Returns the float value
        /// </summary>
        public float FloatValue { get; }

        /// <summary>
        /// Returns the string value
        /// </summary>
        public string StringValue { get; }

        /// <summary>
        /// Returns the parameters used for call instructions 
        /// </summary>
        public IReadOnlyList<VMType> Parameters { get; }

        /// <summary>
        /// Creates a new instruction
        /// </summary>
        /// <param name="opCode">The op code</param>
        public Instruction(OpCodes opCode)
		{
			this.OpCode = opCode;
            this.IntValue = 0;
            this.FloatValue = 0.0f;
            this.StringValue = null;
            this.Parameters = null;
            this.stringRepresentation = $"OpCode: {opCode}";
        }

		/// <summary>
		/// Creates a new instruction
		/// </summary>
		/// <param name="opCode">The op-code</param>
		/// <param name="value">The value</param>
		public Instruction(OpCodes opCode, int value)
		{
			this.OpCode = opCode;
            this.IntValue = value;
            this.FloatValue = 0.0f;
            this.StringValue = null;
            this.Parameters = null;
            this.stringRepresentation = $"OpCode: {opCode}, IntValue: {value}";
        }

        /// <summary>
		/// Creates a new instruction
		/// </summary>
		/// <param name="opCode">The op-code</param>
		/// <param name="value">The value</param>
		public Instruction(OpCodes opCode, float value)
        {
            this.OpCode = opCode;
            this.IntValue = 0;
            this.FloatValue = value;
            this.StringValue = null;
            this.Parameters = null;
            this.stringRepresentation = $"OpCode: {opCode}, FloatValue: {value}";
        }

        /// <summary>
		/// Creates a new instruction
		/// </summary>
		/// <param name="opCode">The op-code</param>
		/// <param name="value">The value</param>
		public Instruction(OpCodes opCode, string value)
        {
            this.OpCode = opCode;
            this.IntValue = 0;
            this.FloatValue = 0.0f;
            this.StringValue = value;
            this.Parameters = null;
            this.stringRepresentation = $"OpCode: {opCode}, StringValue: {value}";
        }

        /// <summary>
		/// Creates a new instruction
		/// </summary>
		/// <param name="opCode">The op-code</param>
		/// <param name="value">The value</param>
        /// <param name="parameters">The parameters</param>
		public Instruction(OpCodes opCode, string value, IList<VMType> parameters)
        {
            this.OpCode = opCode;
            this.IntValue = 0;
            this.FloatValue = 0.0f;
            this.StringValue = value;
            this.Parameters = new ReadOnlyCollection<VMType>(parameters);
            this.stringRepresentation = $"OpCode: {opCode}, StringValue: {value}, Parameters: {string.Join(" ", parameters)}";
        }

        public override string ToString()
        {
            return this.stringRepresentation;
        }
    }
}
