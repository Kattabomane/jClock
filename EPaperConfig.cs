using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;

namespace jclock
{
    public class EPaperConfig : IDisposable
    {
        // Pin definition
        public const int RST_PIN = 17;
        public const int DC_PIN = 25;
        public const int CS_PIN = 8;
        public const int BUSY_PIN = 24;
        public const int PWR_PIN = 18;
        public const int MOSI_PIN = 10;
        public const int SCLK_PIN = 11;

        private GpioController _GPIO;
        private SpiDevice _SPI;

        public EPaperConfig()
        {
            _GPIO = new GpioController(PinNumberingScheme.Logical, new System.Device.Gpio.Drivers.RaspberryPi3Driver());
            var spiConnectionSettings = new SpiConnectionSettings(0, 0)
            {
                Mode = SpiMode.Mode0,
                ClockFrequency = 4000000  // Frequency in Hz
            };
            _SPI = SpiDevice.Create(spiConnectionSettings);

            _GPIO.OpenPin(RST_PIN, PinMode.Output);
            _GPIO.OpenPin(DC_PIN, PinMode.Output);
            _GPIO.OpenPin(CS_PIN, PinMode.Output);
            _GPIO.OpenPin(PWR_PIN, PinMode.Output);
            _GPIO.OpenPin(BUSY_PIN, PinMode.Input);
        }

        public void DigitalWrite(int pinnumber, PinValue pinvalue)
        {
            _GPIO.Write(pinnumber, pinvalue);
        }

        public PinValue DigitalRead(int pinnumber)
        {
            return _GPIO.Read(pinnumber);
        }

        public void DelayMs(int delay)
        {
            Thread.Sleep(delay);
        }

        public void SpiWriteBytes(byte[] data)
        {
            _SPI.Write(data);
        }

        public void SpiWriteByte(byte data)
        {
            _SPI.WriteByte(data);
        }

        public void Dispose()
        {
            if (_SPI != null)
            {
                _SPI.Dispose();
                _SPI = null;
            }

            if (_GPIO != null)
            {
                _GPIO.Write(RST_PIN, PinValue.Low);
                _GPIO.Write(DC_PIN, PinValue.Low);
                _GPIO.Write(PWR_PIN, PinValue.Low);

                _GPIO.ClosePin(RST_PIN);
                _GPIO.ClosePin(DC_PIN);
                _GPIO.ClosePin(CS_PIN);
                _GPIO.ClosePin(PWR_PIN);
                _GPIO.ClosePin(BUSY_PIN);
            }
        }
    }
}