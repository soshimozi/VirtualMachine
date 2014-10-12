using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroAssembler
{
    public class Memory
    {
        private const int MaxAddress = 65535;
        private byte[] _memory = new byte[MaxAddress];

        public event EventHandler<MemoryChangedEventArgs> MemoryChanged;

        public byte this[int address]
        {
            get
            {
                return GetByte(address);
            }

            set
            {
                StoreByte(address, value);
            }
        }

        public byte GetByte(int address)
        {
            CheckAddress(address);
            return _memory[address];
        }

        public int GetWord(int address)
        {
            CheckAddress(address+1);
            return this[address] + (this[address + 1] << 8);
        }

        public void StoreByte(int address, byte value)
        {
            CheckAddress(address);

            _memory[address] = value;

            // notify watchers that we changed
            OnMemoryChanged(address);
        }

        public string Format(int start, int length)
        {
            var html = new StringBuilder();

            for (var x = 0; x < length; x++)
            {
                if ((x & 15) == 0)
                {
                    if (x > 0)
                    {
                        html.Append("\n");
                    }
                    var n = (start + x);
                    html.AppendFormat("{0:X}", ((n >> 8) & 0xff));
                    html.AppendFormat("{0:X}", ((n & 0xff)));
                    html.Append(": ");
                }
                html.AppendFormat("{0:X}", _memory[start + x]);
                html.Append(" ");
            }
            return html.ToString();
        }

        private static void CheckAddress(int address)
        {
            if (address < 0 || address >= MaxAddress)
                throw new AddressOutOfRangeException();
        }

        protected void OnMemoryChanged(int address)
        {
            var func = MemoryChanged;
            if (func != null)
            {
                func(this, new MemoryChangedEventArgs {Address = address});
            }
        }

    }

    public class MemoryChangedEventArgs
    {
        public int Address;
    }

    public class AddressOutOfRangeException : Exception
    {
    }
}
