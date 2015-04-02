using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testTxtEncoding
{
    public class TxtFileEncoding
    {
        /*
原理很简单，文本编辑器（比如XP自带的记事本）在生成文本文件时，
如果编码格式和系统默认的编码（中文系统下默认为GB2312）不一致时，会在txt文件开头部分
添加特定的“编码字节序标识（Encoding Bit Order Madk，简写为BOM）”，类似PE格式的"MZ"文件头。这样
它在读取时就可以根据这个BOM来确定该文本文件生成时所使用的Encoding。这个BOM我们用记事本等程序打开默认
是看不到的，但是用stream按字节读取时是可以读到的。我的这个TxtFileEncoding类就是根据这个BOM“文件头”
来确定txt文件生成时用到的编码的。
由于在GB2312和UTF7编码都没有BOM，所以需要指定一个默认的Encoding，在找不到合法的BOM时，将返回这个Encoding。
         */
        /*
        EF BB BF　　　 UTF-8  
        FE FF　　　　　UTF-16/UCS-2, little endian  
        FF FE　　　　　UTF-16/UCS-2, big endian  
        FF FE 00 00　　UTF-32/UCS-4, little endian.  
        00 00 FE FF　　UTF-32/UCS-4, big-endian.
        */


        /// <summary>
        /// 取得一个文本文件的编码方式。如果无法在文件头部找到有效的前导符，Encoding.Default将被返回。
        /// </summary>
        /// <param name="fileName">文件名。</param>
        /// <returns></returns>
        public static Encoding GetEncoding(string fileName)
        {
            return GetEncoding(fileName, Encoding.Default);
        }
        /// <summary>
        /// 取得一个文本文件流的编码方式。
        /// </summary>
        /// <param name="stream">文本文件流。</param>
        /// <returns></returns>
        public static Encoding GetEncoding(FileStream stream)
        {
            return GetEncoding(stream, Encoding.Default);
        }
        /// <summary>
        /// 取得一个文本文件的编码方式。
        /// </summary>
        /// <param name="fileName">文件名。</param>
        /// <param name="defaultEncoding">默认编码方式。当该方法无法从文件的头部取得有效的前导符时，将返回该编码方式。</param>
        /// <returns></returns>
        public static Encoding GetEncoding(string fileName, Encoding defaultEncoding)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            Encoding targetEncoding = GetEncoding(fs, defaultEncoding);
            fs.Close();
            return targetEncoding;
        }
        /// <summary>
        /// 取得一个文本文件流的编码方式。
        /// </summary>
        /// <param name="stream">文本文件流。</param>
        /// <param name="defaultEncoding">默认编码方式。当该方法无法从文件的头部取得有效的前导符时，将返回该编码方式。</param>
        /// <returns></returns>
        public static Encoding GetEncoding(FileStream stream, Encoding defaultEncoding)
        {

            Encoding targetEncoding = defaultEncoding;
            if (stream != null && stream.Length >= 2)
            {
                //保存当前Seek位置
                long origPos = stream.Seek(0, SeekOrigin.Begin);
                stream.Seek(0, SeekOrigin.Begin);

                BinaryReader r = new BinaryReader(stream, System.Text.Encoding.Default);
                byte[] ss = r.ReadBytes(4);
                stream.Seek(origPos, SeekOrigin.Begin);

                //根据文件流的前4个字节判断Encoding
                //Unicode {0xFF, 0xFE};
                //BE-Unicode {0xFE, 0xFF};
                //UTF8 = {0xEF, 0xBB, 0xBF};
                if (ss[0] == 0xFE && ss[1] == 0xFF )
                {
                    targetEncoding = Encoding.BigEndianUnicode;
                }
                else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] != 0xFF)
                {
                    targetEncoding = Encoding.Unicode;
                }
                else
                {
                    if (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF)
                    {
                        targetEncoding = Encoding.UTF8;
                    }
                    else
                    {
                        int i;
                        int.TryParse(stream.Length.ToString(), out i);
                        ss = r.ReadBytes(i);

                        if (IsUTF8Bytes(ss))
                            targetEncoding = Encoding.UTF8;
                    }
                }

                //恢复Seek位置       
                stream.Seek(origPos, SeekOrigin.Begin);
                r.Close();
            }
            return targetEncoding;
        }

        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        } 

        /// <summary>
        /// 判断是否是不带 BOM 的 UTF8 格式
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1;　 //计算当前正分析的字符应还有的字节数
            byte curByte; //当前分析的字节.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X　
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式!");
            }
            return true;
        }
    }
}
