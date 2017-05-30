using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PropellerManager {
    public class MSCTL {
        MSCStringEditor Editor;
        public MSCTL(byte[] Script) => Editor = new MSCStringEditor(Script);

        Dictionary<int, string> Prefix = new Dictionary<int, string>();

        public string[] Import() {
            string[] Strings = Editor.Import();

            for (int i = 0; i < Strings.Length; i++) {
                string String = Strings[i];
                string Prefix = string.Empty;

                const string TAG = "-\"";
                if (String.Trim().EndsWith("\"") && String.Contains(TAG)) {
                    Prefix = String.Substring(0, String.IndexOf(TAG) + 1);
                    String = String.Substring(Prefix.Length, String.Length - Prefix.Length);
                }

                if (String.Trim().StartsWith("[") && String.Contains("]")) {
                    string Pf = String.Substring(0, String.IndexOf("]") + 1);
                    String = String.Substring(Pf.Length, String.Length - Pf.Length);
                    Prefix += Pf;
                }

                this.Prefix[i] = Prefix;
                Strings[i] = String.Replace("_r", "\n");
            }

            return Strings;
        }

        public byte[] Export(string[] Strings) {
            for (int i = 0; i < Strings.Length; i++) {
                Strings[i] = Prefix[i] + Strings[i].Replace("\n", "_r");
            }

            return Editor.Export(Strings);
        }

    }
}
