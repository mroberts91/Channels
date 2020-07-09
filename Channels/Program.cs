using System;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Channels
{
    class Program
    {
        /*
         * Producer/Consumer Pattern
         * Simulates a long lived stream using System.Threading.Channels
         * The server is producing messages at a arbitray time and
         * the client is reading them all as them come in.
        */
        static readonly Channel<ChannelMessage> channel1 = Channel.CreateUnbounded<ChannelMessage>(new UnboundedChannelOptions 
        {
            AllowSynchronousContinuations = true,
            SingleWriter = true,
            SingleReader = true,
        });

        static readonly Channel<ChannelMessage> channel2 = Channel.CreateUnbounded<ChannelMessage>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleWriter = true,
            SingleReader = true,
        });

        static int count = 0;
        static async Task Main(string[] args)
        {
            var server = new Server<ChannelMessage>(channel1);
            var client1 = new Client<ChannelMessage>(channel1);

            var server2 = new Server<ChannelMessage>(channel2);
            var client2 = new Client<ChannelMessage>(channel2);

            var tasks = new[]
            {
                FirstClientRead(client1),
                SecondClientRead(client2),
                server.ServeMessages(),
                server2.ServeMessages()
            };

            await Task.WhenAll(tasks);

            // Just needed for console app becuase .... well console app.
            while (!server.ChannelClosed && !server2.ChannelClosed) { }
        }

        static async Task FirstClientRead(Client<ChannelMessage> client)
        {
            await foreach (var message in client.ReadMessages())
            {
                count++;
                Console.WriteLine($"\nMessage #{count}");
                Console.WriteLine("Client 1");
                Console.WriteLine(DateTime.Now.ToString());
                Console.WriteLine(message.ToString());
            }
        }

        static async Task SecondClientRead(Client<ChannelMessage> client)
        {
            await foreach (var message in client.ReadMessages())
            {
                count++;
                Console.WriteLine($"\nMessage #{count}");
                Console.WriteLine("Client 2");
                Console.WriteLine(DateTime.Now.ToString());
                Console.WriteLine($"{message}\n");
            }
        }
    }

    class ChannelMessage
    {
        private readonly Guid id;
        private readonly bool updated;
        private readonly int checkInt;
        private static readonly Random rnd = new Random();

        public ChannelMessage()
        {
            id = Guid.NewGuid();
            updated = rnd.Next(1, 11) <= 5;
            checkInt = rnd.Next();
        }

        public override string ToString()
        {
            return $"Channel Message:\n\tID:  {id}\n\tIs Updated:  {updated}\n\tCheck Int:  {checkInt}";
        }
    }

    class Server<T>
    {
        private readonly ChannelWriter<T> writer;

        public bool ChannelClosed { get; set; }

        public Server(Channel<T> channel)
        {
            writer = channel.Writer;
        }

        public async Task ServeMessages()
        {
            var rnd = new Random();
            for (int i = 0; i < 10; i++)
            {
                await writer.WriteAsync(GetTInstance());
                await Task.Delay(rnd.Next(500, 6000));
            }

            writer.Complete();
            ChannelClosed = true;
        }

        private T GetTInstance()
        {
            return (T)Activator.CreateInstance(typeof(T));
        }
    }

    class Client<T>
    {
        private readonly ChannelReader<T> reader;
        public Client(Channel<T> channel)
        {
            reader = channel.Reader;
        }

        public IAsyncEnumerable<T> ReadMessages()
        {
           return reader.ReadAllAsync();
        }
    }
}
