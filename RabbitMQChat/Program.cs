// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

string exchange = "", queueName = "";

Console.Write("Enter host: ");
var host = Console.ReadLine();
if (string.IsNullOrEmpty(host))
{
  host = "localhost";
}

Console.Write("Enter username: ");
var username = Console.ReadLine();

var factory = new ConnectionFactory()
{
  HostName = host, // Change to the appropriate RabbitMQ server address if needed
  UserName = "user",
  Password = "password"
};
var connection = factory.CreateConnection();
var channel = connection.CreateModel();

Start(false);



List<string> messages = new();
Task.Run(Execute);

while (true)
{
  PrintWaitMessage();
  var message = Console.ReadLine();
  var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new MessageModel(username, message)));
  channel.BasicPublish(exchange, routingKey: queueName, basicProperties: null, body: body);
  Console.Clear();
}

void Start(bool singleDelivery = false)
{
  if (singleDelivery)
  {
    queueName = "queue";
    channel.QueueDeclare(queueName, exclusive: false, durable: false);
  }
  else
  {
    exchange = "exchange";
    channel.ExchangeDeclare(exchange, ExchangeType.Fanout);
    var queue = channel.QueueDeclare();
    queueName = queue.QueueName;
    channel.QueueBind(queue, exchange, routingKey: "");
  }
}

void PrintWaitMessage()
{
  var lines = string.Join(Environment.NewLine, messages);
  Console.Clear();
  Console.WriteLine(lines);
  Console.CursorTop = Console.WindowHeight - 1;
  Console.Write("Enter message: ");
}
 
void Execute()
{
  var consumer = new EventingBasicConsumer(channel);
  consumer.Received += (model, ea) =>
  {
    var message = JsonSerializer.Deserialize<MessageModel>(Encoding.UTF8.GetString(ea.Body.Span));

    messages.Add("[" + message.UserName + "]: " + message.Content);
    // Handle the message (e.g., log it)
    Console.CursorTop = 0;
    PrintWaitMessage();
  };

  channel.BasicConsume(queueName, autoAck: false, consumer: consumer);
}

record MessageModel(string UserName, string Content);