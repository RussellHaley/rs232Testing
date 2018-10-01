using System;
using System.Text;
using RJCP.IO.Ports;
using Neo.IronLua;
using System.Collections.Generic;
using System.IO;
using LiteDB;
namespace rs232Testing
{
    class Record
    {
        public int Id { get; set; }
        public List<string> Messages { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Span { get; set; }

        public Record()
        {
            Messages = new List<string>();
        }
    }

    class Program
    {
        public delegate void Del(Record rec);
        static void Main(string[] args)
        {
            Check();
            Run();
        }

        public static void Check()
        {
            using (var db = new LiteDatabase(@"MyData.db"))
            {
                var col = db.GetCollection<Record>("records");

                var count = col.Count(x => x.StartTime > new DateTime(2018, 09, 30, 19, 55, 00) && x.StartTime <= new DateTime(2018, 09, 30, 20, 14, 00));
                
                //var results = col.Find(x => x.StartTime > new DateTime(2018, 09, 30, 19, 55, 00) && x.StartTime <= new DateTime(2018, 09, 30, 20, 15, 00));

                var total = col.Count();

                Console.WriteLine("{0} of {1}", count, total);
                var results = col.FindAll();
                foreach (Record r in results)
                {
                    Console.Write("{0} - {1} - {2}\r\n", r.Id, r.StartTime, r.Span);
                    foreach (var m in r.Messages)
                    {
                        Console.WriteLine(m);
                    }
                }

            }

        }
        public static void Run()
        {
            DateTime start = DateTime.Now;
            Console.WriteLine("Started {0}", start.ToLocalTime());
            using (var db = new LiteDatabase(@"MyData.db"))
            {
                var col = db.GetCollection<Record>("records");
                col.EnsureIndex(i => i.StartTime, true);
                Console.WriteLine("Database loaded {0}", DateTime.Now.Subtract(start));
                Lua l = new Lua();

                dynamic g = l.CreateEnvironment<LuaGlobal>();
                using (TextReader reader = File.OpenText(@"C:\Users\russh\source\repos\rs232Testing\rs232Testing\Lua\MessageHandler.lua"))
                {
                    ((LuaGlobal)g).DoChunk(reader, "OnMsg.lua", null);
                }
                Del x = (Record rec) => { col.Insert(rec); };
                g.Save = x;
                Console.WriteLine("Lua loaded {0}", DateTime.Now.Subtract(start));
                //string contents = File.ReadAllText(@"C:\Users\russh\source\repos\rs232Testing\rs232Testing\Lua\MessageHandler.lua");
                //g.dochunk(contents, "OnMsg.lua");

                //g.message_received("Test1");
                string test = "Testing Lua Output... OK";
                //g.message_received(test);
                string com = "COM4";
                //Console.WriteLine("{0}, {1}", args[0], args[1]);
                byte[] readBuffer = new byte[8192];
                byte[] writeBuffer = new byte[8192];

                using (SerialPortStream src = new SerialPortStream(com, 115200))
                {
                    bool running = true;
                    Console.WriteLine("Starting COM {0} {1}", com, DateTime.Now.Subtract(start));
                    src.Open();
                    Console.WriteLine("COM open {0}", DateTime.Now.Subtract(start));
                    int totalBytes = 0;
                    int count = 0;
                    src.DataReceived += (s, e) =>
                    {

                        int bytes = src.Read(readBuffer, 0, readBuffer.Length);
                        byte[] buf = new byte[bytes];
                        Buffer.BlockCopy(readBuffer, 0, buf, 0, bytes);
                        totalBytes += bytes;
                        count++;
                        string str = Encoding.ASCII.GetString(buf);

                    //g.message_received(str);
                        g.data_received(str);
                        //Console.WriteLine("===> EventType: {0}, bytes read = {1}, total read = {2}", e.EventType, bytes, totalBytes);
                        //if (totalBytes >= testTotalBytes) finished.Set();
                    };
                    src.ErrorReceived += (s, e) =>
                    {
                        Console.WriteLine("===> EventType: {0}", e.EventType);
                    };

                    Console.WriteLine("Hello World!");
                    string input;
                    while (running)
                    {
                        input = Console.ReadLine();

                        if (input == "q")
                        {
                            running = false;
                            g.end_program();
                        }
                        else
                        {
                            src.Write(input + "\n");
                        }
                    }

                    Console.WriteLine("Total Bytes: {0}", totalBytes);
                    Console.ReadKey();
                }
            }
        }
    }
}
