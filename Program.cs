using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LZWconsole
{
    class LZW_converter
    {
        private string filePath;

        private List<UInt16> outputCode = new List<UInt16>();
        private byte[] fileData = null;//прочитанный файл

        private Dictionary<UInt16, string> dic = new Dictionary<UInt16, string>();//статичный словарь

        public LZW_converter(string path)
        {
            filePath = path;
            ReadFile(path);
            GetPrStaticDic();
        }

        //Читает файл по заданному пути
        public void ReadFile(string path)
        {
            try
            {   
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fs, Encoding.GetEncoding(1251)))
                    {
                        fileData = binaryReader.ReadBytes((int)fs.Length);
                    }
                }
                Console.WriteLine("\nThe file was successfully read!\n");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //Формирование первичного статичного словаря 0-255
        public void GetPrStaticDic()
        {
            for (int i = 0; i < 256; i++)
            {
                dic.Add(Convert.ToUInt16(i), Convert.ToChar(i).ToString());
            }
        }

        //Алгоритм кодирования; на вход поступает информация из файла, возвращается список кодов
        public void LZWencoding()
        {
            string p = "";

            UInt16 k = 256;//индекс словаря
            for (int j = 0; j < fileData.Length; j++)//идем по входной строке
            {
                char c = Convert.ToChar(fileData[j]);//с- след символ в потоке символов
                string sum = p + c;

                if (dic.ContainsValue(sum)) //если строка p+c присутствует в словаре
                    p = sum;
                else
                {
                    dic.Add(k, sum);//добавили строку p+c в словарь на позицию k
                    k++;

                    //выводим кодовое слово, которое соответствует значению p входного потока (у нас: вывод ключа значения p)
                    outputCode.Add(Convert.ToUInt16(dic.Values.ToList().IndexOf(p)));
                    p = Convert.ToString(c);
                }
            }
            //когда входная строка закончилась, записываем кодовое слово, соответствующее последнему p
            outputCode.Add(Convert.ToUInt16(dic.Values.ToList().IndexOf(p)));//у нас индекс p в списке совпадает с ключом в словаре   
        }

        //Запись списка кодов в бинарник. Закодированный файл создается в той же директории, из которой считан исх файл
        public void WriteFile()
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Create(filePath + ".lzw"), Encoding.Unicode))
                {
                    for (int i = 0; i < outputCode.Count; i++)
                    {
                        byte[] ar = BitConverter.GetBytes(outputCode.ElementAt(i));
                        writer.Write(ar, 0, 2);
                    }
                }
                Console.WriteLine("The encoded file\n" + filePath + ".lzw" + " was successfully created!\n");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //При декодировании отбрасывает расширение файла, добавляет приставку _dec к имени файла
        public void GetFilePath()
        {
            filePath = filePath.Insert(filePath.IndexOf("."), "_dec");
            filePath = filePath.Substring(0, filePath.IndexOf(".lzw"));
        }

        //Декодирование бинарного файла. 
        //Здесь же создание и запись раскодированного файла, чтобы не передавать большую строку в отдельную ф-ю
        public void LZWdecoding()
        {
            int j = 0;
            while (j < fileData.Length)
            {
                byte[] ar = new byte[2];
                ar[0] = fileData[j];
                ar[1] = fileData[j + 1];
                UInt16 value = BitConverter.ToUInt16(ar, 0);
                outputCode.Add(value);
                j += 2;
            }

            UInt16 k = 256;
            string sfileData = "";

            UInt16 ccode = outputCode.ElementAt(0);//с-первое кодовое слово

            sfileData += dic.Values.ElementAt(ccode);//Выводим в строку знак, соответствующий кодовому слову с

            for (int i = 1; i < outputCode.Count; i++)//идем по кодовой последовательности
            {
                int pcode = ccode;
                ccode = outputCode.ElementAt(i);//с-следующее кодовое слово
                if (dic.ContainsKey(ccode))
                {
                    sfileData += dic.Values.ElementAt(ccode);//Выводим в строку знак, соответствующий кодовому слову с
                    string p = dic.Values.ElementAt(pcode);
                    string c = dic.Values.ElementAt(ccode).Substring(0, 1);
                    string sum = p + c;
                    dic.Add(k, sum);
                    k++;
                }
                else
                {
                    string p = dic.Values.ElementAt(pcode);
                    string c = dic.Values.ElementAt(pcode).Substring(0, 1);
                    string sum = p + c;
                    sfileData += sum;
                    dic.Add(k, sum);
                    k++;
                }
            }  
            GetFilePath();

            //Запись раскодированной информации в файл
            byte[] bfileData = Encoding.Unicode.GetBytes(sfileData);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Create(filePath), Encoding.GetEncoding(1251)))
                {
                    int n = 0;
                    //цикл для удаления лишних нулевых байтов, которые появились при конвертации кодировок
                    while (n < bfileData.Length)
                    {
                        byte[] ar = new byte[2];
                        ar[0] = bfileData[n];
                        ar[1] = bfileData[n + 1];
                        if (ar[1] == 0) writer.Write(ar[0]);
                        else writer.Write(ar);
                        n += 2;
                    }            
                }
                Console.WriteLine("The decoded file\n" + filePath + " was successfully created!\n");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\nWelcome to the LZW archiver!\n");
            Console.WriteLine("Enter:\n1 - to encode file;\n2 - to decode file;\n3 - to exit\n");
            string answer = Console.ReadLine();

            switch (answer)
            {
                case "1":
                    Console.WriteLine("\nENCODING\n");
                    Console.WriteLine("Enter the file path:\n");
                    string filepath = Console.ReadLine();

                    LZW_converter converter1 = new LZW_converter(filepath);

                    converter1.LZWencoding();

                    converter1.WriteFile();
                    break;

                case "2":
                    Console.WriteLine("\nDECODING\n");
                    Console.WriteLine("Enter the file path:\n");
                    string encfilepath = Console.ReadLine();

                    LZW_converter converter2 = new LZW_converter(encfilepath);

                    converter2.LZWdecoding();
                    break;

                case "3":
                    Console.ReadKey();
                    break;

                default:
                    Console.WriteLine("Please enter 1, 2 or 3 number!\n");
                    break;
            }
            Console.ReadKey();
        }
    }
}