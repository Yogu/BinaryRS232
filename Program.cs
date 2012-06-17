using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Globalization;

namespace Yogu.BinaryRs232 {
	class Program {
		static SerialPort port;

		static void Main(string[] args) {
			try {
				Console.Title = "Yogus Binary RS232 terminal";
				Console.WriteLine("Binary RS232 terminal");
				new Program().Start();
			} catch (Exception e) {
				Console.WriteLine("Unhandled exception occurred: " + e.Message);
				Console.WriteLine(e.StackTrace);
				Console.ReadKey();
			}
		}

		private void Start() {
			do {
				port = new SerialPort();
				port.PortName = "COM" + askForPortNumber();
				port.BaudRate = askForBaudRate();
				try {
					port.Open();
				} catch (IOException e) {
					Console.WriteLine("Failed to open com port: " + e.Message);
				} catch (UnauthorizedAccessException e) {
					Console.WriteLine("Access to this com port refused: " + e.Message);
				}
			} while (!port.IsOpen);
			Console.WriteLine("Opened port, enter numbers to send bytes; enter \"q\" to quit or \"c\" to clear the window.");
			Run();
		}

		private void Run() {
			port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
			while (true) {
				string line = Console.ReadLine();
				switch (line) {
					case "q":
						return;
					case "c":
						Console.Clear();
						break;
					default:
						var numbers = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
						foreach (String str in numbers) {
							int number;
							if (parseNumber(str, out number)) {
								if (number >= 0 && number <= 255) {
									try {
										port.Write(new byte[] { (byte)number }, 0, 1);
									} catch (IOException e) {
										Console.WriteLine("Exception writing byte: " + e.Message);
									}
								} else
									Console.WriteLine(str + ": Number must be between 0 and 255");
							} else
								Console.WriteLine(str + ": Not a number.");
						}
						break;
				}
			}
		}

		private int askForPortNumber() {
			Console.Write("Port Number? ");
			string line = Console.ReadLine();
			int number;
			if (!int.TryParse(line, out number) || number <= 0) {
				Console.WriteLine("Please enter a positive integer");
				return askForPortNumber();
			} else {
				if (SerialPort.GetPortNames().Contains("COM" + number))
					return number;
				else {
					Console.WriteLine("There is no such serial port on this computer.");
					return askForPortNumber();
				}
			}
		}

		private int askForBaudRate() {
			SerialPort dummyPort = new SerialPort();
			Console.Write("Baud Rate? ");
			string line = Console.ReadLine();
			int number;
			if (!int.TryParse(line, out number) || number <= 0) {
				Console.WriteLine("Please enter a positive integer");
				return askForBaudRate();
			} else {
				try {
					port.BaudRate = number;
				} catch (Exception e) {
					if (e is IOException || e is ArgumentOutOfRangeException) {
						Console.WriteLine("The baud rate you enterred is not supported");
						return askForBaudRate();
					} else
						throw;
				}
				return number;
			}
		}

		private static bool parseNumber(string input, out int number) {
			bool isHex = false;
			int hexPrefixLength = 0;
			if (input[0] == '$') {
				isHex = true;
				hexPrefixLength = 1;
			} else if (input.StartsWith("0x")) {
				isHex = true;
				hexPrefixLength = 2;
			}

			if (isHex)
				input = input.Substring(hexPrefixLength);

			if (!isHex && int.TryParse(input, out number))
				return true;

			return int.TryParse(input, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture.NumberFormat, out number);
		}

		static void port_DataReceived(object sender, SerialDataReceivedEventArgs e) {
			while (port.BytesToRead > 0) {
				int value;
				try {
					value = port.ReadByte();
				} catch (IOException ex) {
					Console.WriteLine("Exception reading byte: " + ex.Message);
					continue;
				}
				Console.WriteLine("Read: {0:X02}  {2}  {0,3}  {1}", value, (char)value, Convert.ToString(value, 2).PadLeft(8, '0'));
			}
		}
	}
}
