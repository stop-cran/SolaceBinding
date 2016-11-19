﻿using System;

namespace JsonRpcOverTcp.SimpleServer
{
    public class CalculatorService
    {
        public int Add(int x, int y) { return x + y; }
        public int Subtract(int x, int y) { return x - y; }
        public int Multiply(int x, int y) { return x * y; }
        public int Divide(int x, int y)
        {
            try
            {
                return x / y;
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error dividing", e);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SocketsServer server = new SocketsServer(8000, new CalculatorService());
            server.StartServing();

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();

            server.StopServing();
        }
    }
}
