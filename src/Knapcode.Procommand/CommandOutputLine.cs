using System;

namespace Knapcode.Procommand
{
    public class CommandOutputLine : IEquatable<CommandOutputLine>
    {
        public CommandOutputLine(CommandOutputLineType type, string value)
        {
            Type = type;
            Value = value;
        }

        public CommandOutputLineType Type { get; set; }
        public string Value { get; set; }

        public override int GetHashCode()
        {
            var hashCode = Type.GetHashCode();
            if (Value != null)
            {
                hashCode *= 17;
                hashCode += Value.GetHashCode();
            }

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CommandOutputLine);
        }

        public bool Equals(CommandOutputLine other)
        {
            if (other == null)
            {
                return false;
            }

            return Type == other.Type &&
                   Value == other.Value;
        }
    }
}
