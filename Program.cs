/*
 Ленский Вячеслав - Тестовое задание
 Язык разработки C# на выбор, под ОС Windows реализовать следующий функционал: 
на выбор консолькое приложение/служба , осуществляющая мониторинг очереди печати принтера;
обнаружении документа в очереди вывести его имя и количество распечатываемых страниц 
(для консоли соответственно в консоль, для службы в лог файл/ просто в файл);
для каждого распечатываемого документа информация должна выводиться в соответствии с количеством распечатываемых копий.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Printing;

namespace PrintingConsoleService
{
    class Program
    {
        // Интервал мониторинга
        private const int SLEEP_TIME = 1;
        private static Dictionary<int, Document> dictOfDocs;

        public static void Main(string[] args)
        {

            // Словарь: Ключ - JobIdentifier, Значение - объект типа Document(имя, имя принтера, кол-во страниц, кол-во копий)
            dictOfDocs = new Dictionary<int, Document>();

            // Создание фонового потока
            BackgroundWorker backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerAsync();

            // Выход при вводе с клавиатуры
            Console.ReadKey();
        }

        // Метод фонового потока
        private static void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            LocalPrintServer server = new LocalPrintServer();

            // Вечный цикл ¯\_(ツ)_/¯        
            while(true)
            {
                server.Refresh();

                // Директория Spool, содержащая очереди на печать
                string[] initDirs = Directory.GetFiles(server.DefaultSpoolDirectory);

                // Если папка не пуста,
                if (initDirs.Length > 0)
                {
                    // Обновить и отобразить текущую информацию о печати
                    UpdateData(server);                
                } 
                else
                {
                    // Сброс словаря dictOfDocs
                    dictOfDocs.Clear();                  
                }

                // Доброй ночи
                System.Threading.Thread.Sleep(SLEEP_TIME);
            }
        }

        //
        private static void UpdateData(LocalPrintServer server)
        {
            server.Refresh();

            // Коллекция принтеров
            PrintQueueCollection printQueueCollection = server.GetPrintQueues();

            // Цикл по всем локальным принтерам
            foreach (PrintQueue printQueue in printQueueCollection)
            {
                printQueue.Refresh();

                // Если количество документов в принтере > 0
                if (printQueue.NumberOfJobs != 0)
                {
                    // Коллекция задач печати текущего принтера
                    var jobCollection = printQueue.GetPrintJobInfoCollection();                             

                    // Цикл по всем документам принтера
                    foreach(var job in jobCollection)
                    {
                        // Если в словаре dictOfDocs уже содержится Id текущей задачи
                        if (dictOfDocs.ContainsKey(job.JobIdentifier))
                        {
                            // Обновляем число страниц (от числа уже напечатанных)
                            dictOfDocs[job.JobIdentifier].NumberOfPages = job.NumberOfPagesPrinted;
                        } 
                        else
                        {
                            // Добавляем в словарь новый элемент. Ключ - Id задачи, Значение - Объект типа Document
                            dictOfDocs.Add(job.JobIdentifier, new Document(job.Name, printQueue.Name, job.NumberOfPagesPrinted, (int)printQueue.DefaultPrintTicket.CopyCount));
                        }                     
                    }
                    
                    // Обновление информации в командной строке
                    PrintData();
                }        
            }           
        }

        // Обновление информации в командной строке
        private static void PrintData()
        {
            Console.Clear();

            // Цикл по каждому значению (объект класса Document) в словаре dictOfDocs
            foreach (Document doc in dictOfDocs.Values)
            {
                doc.WriteProperties();
            }
        }
    }

    // Класс печатаемого документа: имя, имя принтера, кол-во страниц, кол-во копий.
    class Document
    {
        private string Name;
        private int CopyCount;
        private string PrinterName;

        private int pages;
        public int NumberOfPages
        { 
            get { return pages; }

            // Сеттер свойства NumberOfPages. Меняется только если входное значение больше.
            set { pages = (pages >= value) ? pages : value; }
        }

        public Document(string name, string printerName, int pages, int copies)
        {
            NumberOfPages = pages;
            Name = name;
            PrinterName = printerName;
            CopyCount = copies;
        }

        // Метод печати полей объекта класса Document в консоль
        public void WriteProperties()
        {
            Console.WriteLine("Printer: " + PrinterName);
            Console.WriteLine("Name: " + Name + "\tNumber of Pages: " + (NumberOfPages + 1) + "\tCopies: " + CopyCount);
            Console.WriteLine();
        }
    }
}