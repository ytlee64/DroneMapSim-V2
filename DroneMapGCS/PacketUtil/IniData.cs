using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PacketUtil
{
    public static class IniData
    {
        #region >> ini 파일 엑세스
        // iniSet("AAA", "B1", "CCC");   // ini 파일에 쓰기
        // iniGet("AAA", "B1");   // ini 파일에 읽기
        // iniSet("AAA", null, null); 섹션 초기화

        static string iniPath = Environment.CurrentDirectory + @"\config.ini";   // ini 파일명
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        // ini파일에 쓰기
        public static void iniSet(string _Section, string _Key, string _Value)
        {
            WritePrivateProfileString(_Section, _Key, _Value, iniPath);
        }

        // ini파일 값 가져오기
        public static string iniGet(string _Section, string _Key, string _Def)
        {
            StringBuilder STBD = new StringBuilder(1000);
            GetPrivateProfileString(_Section, _Key, _Def, STBD, 5000, iniPath);
            if(STBD.ToString()==_Def)
            {
                iniSet(_Section, _Key, _Def);
            }
            return STBD.ToString().Trim();
        }
        #endregion
    }
}
