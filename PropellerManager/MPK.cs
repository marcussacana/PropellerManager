using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VirtualStream;

namespace PropellerManager {
    public class MPK {
        internal byte KEY;
        internal bool Initialized = false;
        const int SJIS = 932;
        private StructReader Packget;
        private Encoding Enco = Encoding.GetEncoding(SJIS);
        public MPK(Stream Packget) {
            this.Packget = new StructReader(Packget);
        }

        public MPK(Stream Packget, byte KEY) {
            this.Packget = new StructReader(Packget);
            this.KEY = KEY;
            Initialized = true;
        }

        public MPKEntry[] Open() {
            Packget.BaseStream.Position = 0;
            MPKHeader Header = new MPKHeader();
            Packget.ReadStruct(ref Header);

            Packget.BaseStream.Position = Header.OffsetTablePos;

            List<MPKEntry> Entries = new List<MPKEntry>();
            for (int i = 0; i < Header.EntryCount; i++) {
                FileEntry Entry = new FileEntry();
                Entry.RXOR = XOR;

                Packget.ReadStruct(ref Entry);
                string Name = Enco.GetString(Entry.FileNameBuffer);
                Name = Name.Replace("\x0", "");
                Name = Name.Substring(1, Name.Length-1);
                MPKEntry File = new MPKEntry() {
                    FileName = Name,
                    Content = new VirtStream(Packget.BaseStream, Entry.Pos, Entry.Len)
                };

                Entries.Add(File);
            }

            return Entries.ToArray();
        }

        public void Repack(Stream Output, MPKEntry[] Files, bool CloseStreams) {
            if (!Initialized)
                throw new Exception("You need open a packget before repack to catch the encryption key.");

            StructWriter Writer = new StructWriter(Output);
            MPKHeader Header = new MPKHeader();
            Writer.WriteStruct(ref Header);//Write Null Header

            FileEntry[] Entries = new FileEntry[Files.Length];
            for (int i = 0; i < Files.Length; i++) {
                FileEntry Entry = new FileEntry() {
                    WXOR = XOR,
                    FileNameBuffer = Enco.GetBytes(Files[i].FileName),
                    Pos = (uint)Writer.BaseStream.Position,
                    Len = (uint)Files[i].Content.Length
                };
                Entries[i] = Entry;

                Tools.CopyStream(Files[i].Content, Writer.BaseStream);
                if (CloseStreams)
                    Files[i].Content.Close();
            }

            Header.EntryCount = (uint)Entries.LongLength;
            Header.OffsetTablePos = (uint)Writer.BaseStream.Position;
            Writer.BaseStream.Position = 0;
            Writer.WriteStruct(ref Header);
            Writer.BaseStream.Position = Header.OffsetTablePos;

            foreach (FileEntry Entry in Entries) {
                FileEntry TMP = Entry;
                Writer.WriteStruct(ref TMP);
            }
            if (CloseStreams)
                Output.Close();
        }
        private dynamic XOR(Stream Data, bool Reading, dynamic Instance) {
            if (!Initialized) {
                KEY = Instance.FileNameBuffer[0x1F];
                Initialized = true;
            }

            if (Instance.FileNameBuffer.Length < 32) {
                byte[] tmp = new byte[32];
                Instance.FileNameBuffer.CopyTo(tmp, 0);
                Instance.FileNameBuffer = tmp;
            }

            for (int i = 0; i < Instance.FileNameBuffer.Length; i++)
                Instance.FileNameBuffer[i] ^= KEY;

            uint DWKey = BitConverter.ToUInt32(new byte[] { KEY, KEY, KEY, KEY }, 0);
            Instance.Len ^= DWKey;
            Instance.Pos ^= DWKey;

            return Instance;
        }
    }
    
    internal struct MPKHeader{
        internal uint OffsetTablePos;
        internal uint EntryCount;
    }

    internal struct FileEntry {
        internal FieldInvoke WXOR;
        [ArraySize(Length = 32)]
        internal byte[] FileNameBuffer;
        internal uint Pos;
        internal uint Len;

        internal FieldInvoke RXOR;
    }
    public struct MPKEntry { 
        public string FileName;
        public Stream Content;
    }
}
