using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PropellerManager {
    public class MSCStringEditor {

        //public Encoding Encoding = Encoding.GetEncoding(932);//sjis
        public Encoding Encoding = Encoding.Default;//ansi, wtf


        byte[] Script;
        public MSCStringEditor(byte[] Script) => this.Script = Script;

        List<uint> Offsets;
        public string[] Import() {
            List<string> Strings = new List<string>();
            Offsets = new List<uint>();
            uint AOP = 0;
            byte[] OP = GetStrOp((uint)Strings.LongCount());

            for (uint i = 0; i < Script.Length; i++) {
                if (AOP != Strings.LongCount()) {
                    AOP++;
                    OP = GetStrOp(AOP);
                }

                if (EqualsAt(Script, OP, i)) {
                    i += (uint)OP.Length;
                    Offsets.Add(i);
                    string String = string.Empty;
                    try { GetString(out String, ref i); }
                    catch { }
                    if (string.IsNullOrWhiteSpace(String) || String.Contains("\u0006") || String.Contains("\x0") || String == "\\n") {
                        Offsets.RemoveAt(Offsets.Count - 1);
                        continue;
                    }
                    Strings.Add(String);
                }
            }

            return Strings.ToArray();
        }

        public byte[] Export(string[] Strings) {
            byte[] OutScript = new byte[Script.Length];
            Script.CopyTo(OutScript, 0);

            for (int i = Strings.Length - 1; i >= 0; i--) {
                uint Offset = Offsets[i];
                uint Len = GetStringLength(Offset);
                OutScript = CutRegion(OutScript, Offset, Len);
                byte[] String = CompileString(Strings[i]);
                OutScript = InsertArray(OutScript, String, Offset);
            }

            return OutScript;
        }

        private byte[] CompileString(string String) {
            byte[] StringData = Encoding.GetBytes(String);
            byte[] Out = new byte[StringData.Length + 4];
            BitConverter.GetBytes(StringData.Length).CopyTo(Out, 0);
            StringData.CopyTo(Out, 4);
            return Out;
        }

        private void GetString(out string String, ref uint Index) {
            byte[] DW = new byte[4];
            for (int i = 0; i < 4; i++)
                DW[i] = Script[Index++];

            uint Len = BitConverter.ToUInt32(DW, 0);

            byte[] Buffer = new byte[Len];
            for (uint i = 0; i < Len; i++)
                Buffer[i] = Script[Index++];
            Index--;
            String = Encoding.GetString(Buffer);
        }

        private uint GetStringLength(uint Index) {
            byte[] DW = new byte[4];
            for (int i = 0; i < 4; i++)
                DW[i] = Script[Index++];

            return 4 + BitConverter.ToUInt32(DW, 0);
        }

        private byte[] GetStrOp(uint i) {
            byte[] Arr = new byte[7];
            (new byte[] { 0x05, 0x00, 0x00 }).CopyTo(Arr, 0);
            BitConverter.GetBytes(i).CopyTo(Arr, 3);
            return Arr;
        }

        #region ArrayOperations
        private bool EqualsAt(byte[] Data, byte[] DataToCompare, uint Pos) {
            if (DataToCompare.LongLength + Pos > Data.LongLength)
                return false;
            for (uint i = 0; i < DataToCompare.LongLength; i++)
                if (Data[i + Pos] != DataToCompare[i])
                    return false;
            return true;
        }
        private byte[] InsertArray(byte[] Data, byte[] DataToInsert, uint Pos) {
            byte[] tmp = CutAt(Data, Pos);
            byte[] tmp2 = CutAfter(Data, Pos);
            byte[] Rst = new byte[Data.Length + DataToInsert.Length];
            tmp.CopyTo(Rst, 0);
            DataToInsert.CopyTo(Rst, tmp.Length);
            tmp2.CopyTo(Rst, tmp.Length + DataToInsert.Length);
            return Rst;
        }
        private byte[] CutRegion(byte[] Data, uint pos, uint length) {
            byte[] tmp = CutAt(Data, pos);
            byte[] tmp2 = CutAfter(Data, pos + length);
            byte[] rst = new byte[tmp.Length + tmp2.Length];
            tmp.CopyTo(rst, 0);
            tmp2.CopyTo(rst, tmp.Length);
            return rst;
        }
        private byte[] CutAt(byte[] data, uint pos) {
            byte[] rst = new byte[pos];
            for (uint i = 0; i < pos; i++)
                rst[i] = data[i];
            return rst;
        }
        private byte[] CutAfter(byte[] data, uint pos) {
            byte[] rst = new byte[data.Length - pos];
            for (uint i = pos; i < data.Length; i++)
                rst[i - pos] = data[i];
            return rst;
        }
        #endregion
    }
}
