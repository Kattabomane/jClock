using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace jclock
{
    public class EPaperDisplay2in7
    {
        public const int EPD_WIDTH = 176;
        public const int EPD_HEIGHT = 264;

        private EPaperConfig _EPCfg;

        private byte[] lut_vcom_dc = { 0x00, 0x00,
                0x00, 0x08, 0x00, 0x00, 0x00, 0x02,
                0x60, 0x28, 0x28, 0x00, 0x00, 0x01,
                0x00, 0x14, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x12, 0x12, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private byte[] lut_ww = {
                0x40, 0x08, 0x00, 0x00, 0x00, 0x02,
                0x90, 0x28, 0x28, 0x00, 0x00, 0x01,
                0x40, 0x14, 0x00, 0x00, 0x00, 0x01,
                0xA0, 0x12, 0x12, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private byte[] lut_bw = {
                0x40, 0x08, 0x00, 0x00, 0x00, 0x02,
                0x90, 0x28, 0x28, 0x00, 0x00, 0x01,
                0x40, 0x14, 0x00, 0x00, 0x00, 0x01,
                0xA0, 0x12, 0x12, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private byte[] lut_bb = {
                0x80, 0x08, 0x00, 0x00, 0x00, 0x02,
                0x90, 0x28, 0x28, 0x00, 0x00, 0x01,
                0x80, 0x14, 0x00, 0x00, 0x00, 0x01,
                0x50, 0x12, 0x12, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        private byte[] lut_wb = {
                0x80, 0x08, 0x00, 0x00, 0x00, 0x02,
                0x90, 0x28, 0x28, 0x00, 0x00, 0x01,
                0x80, 0x14, 0x00, 0x00, 0x00, 0x01,
                0x50, 0x12, 0x12, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        public EPaperDisplay2in7()
        {
            _EPCfg = new EPaperConfig();
        }

        public void Reset()
        {
            _EPCfg.DigitalWrite(EPaperConfig.RST_PIN, PinValue.High);
            _EPCfg.DelayMs(200);
            _EPCfg.DigitalWrite(EPaperConfig.RST_PIN, PinValue.Low);
            _EPCfg.DelayMs(5);
            _EPCfg.DigitalWrite(EPaperConfig.RST_PIN, PinValue.High);
            _EPCfg.DelayMs(200);
        }

        public void SendCommand(byte command)
        {
            _EPCfg.DigitalWrite(EPaperConfig.DC_PIN, PinValue.Low);
            _EPCfg.DigitalWrite(EPaperConfig.CS_PIN, PinValue.Low);
            _EPCfg.SpiWriteByte(command);
            _EPCfg.DigitalWrite(EPaperConfig.CS_PIN, PinValue.High);
        }

        public void SendData(byte data)
        {
            _EPCfg.DigitalWrite(EPaperConfig.DC_PIN, PinValue.High);
            _EPCfg.DigitalWrite(EPaperConfig.CS_PIN, PinValue.Low);
            _EPCfg.SpiWriteByte(data);
            _EPCfg.DigitalWrite(EPaperConfig.CS_PIN, PinValue.High);
        }

        public void ReadBusy()
        {
            while (_EPCfg.DigitalRead(EPaperConfig.BUSY_PIN) == 1)      //  0: idle, 1: busy
                _EPCfg.DelayMs(200);
        }

        public void Clear(byte color = 0XFF)
        {
            SendCommand(0x10);
            for (int i = 0; i < (int)(EPD_WIDTH * EPD_HEIGHT / 8); i++)
                SendData(color);
            SendCommand(0x13);
            for (int i = 0; i < (int)(EPD_WIDTH * EPD_HEIGHT / 8); i++)
                SendData(color);
            SendCommand(0x12);
            ReadBusy();
        }

        public void SetLut()
        {
            SendCommand(0x20); // vcom
            for (int i = 0; i < 44; i++)
                SendData(lut_vcom_dc[i]);
            SendCommand(0x21); // ww
            for (int i = 0; i < 42; i++)
                SendData(lut_ww[i]);
            SendCommand(0x22); // ww
            for (int i = 0; i < 42; i++)
                SendData(lut_bw[i]);
            SendCommand(0x23); // ww
            for (int i = 0; i < 42; i++)
                SendData(lut_bb[i]);
            SendCommand(0x24); // ww
            for (int i = 0; i < 42; i++)
                SendData(lut_wb[i]);
        }

        public void Init()
        {
            Reset();

            SendCommand(0x01);      // POWER_SETTING
            SendData(0x03);         // VDS_EN, VDG_EN
            SendData(0x00);         // VCOM_HV, VGHL_LV[1], VGHL_LV[0]
            SendData(0x2b);         // VDH
            SendData(0x2b);         // VDL
            SendData(0x09);         // VDHR      

            SendCommand(0x06);      // BOOSTER_SOFT_START
            SendData(0x07);
            SendData(0x07);
            SendData(0x17);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x60);
            SendData(0xA5);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x89);
            SendData(0xA5);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x90);
            SendData(0x00);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x93);
            SendData(0x2A);

            // Power optimization
            SendCommand(0xF8);
            SendData(0xA0);
            SendData(0xA5);

            // Power optimization
            SendCommand(0xF8);
            SendData(0xA1);
            SendData(0x00);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x73);
            SendData(0x41);

            SendCommand(0x16);      // PARTIAL_DISPLAY_REFRESH
            SendData(0x00);
            SendCommand(0x04);      // POWER_ON
            ReadBusy();

            SendCommand(0x00);      // PANEL_SETTING
            SendData(0xAF);         // KW-BF   KWR-AF    BWROTP 0f

            SendCommand(0x30);      // PLL_CONTROL
            SendData(0x3A);         // 3A 100HZ   29 150Hz 39 200HZ    31 171HZ

            SendCommand(0X50);	    // VCOM AND DATA INTERVAL SETTING			
            SendData(0x57);

            SendCommand(0x82);      // VCM_DC_SETTING_REGISTER
            SendData(0x12);
            SetLut();
        }

        public void Init4Gray()
        {
            Reset();

            SendCommand(0x01);      // POWER_SETTING
            SendData(0x03);         // VDS_EN, VDG_EN
            SendData(0x00);         // VCOM_HV, VGHL_LV[1], VGHL_LV[0]
            SendData(0x2b);         // VDH
            SendData(0x2b);         // VDL 

            SendCommand(0x06);      // BOOSTER_SOFT_START
            SendData(0x07);
            SendData(0x07);
            SendData(0x17);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x60);
            SendData(0xA5);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x89);
            SendData(0xA5);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x90);
            SendData(0x00);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x93);
            SendData(0x2A);

            // Power optimization
            SendCommand(0xF8);
            SendData(0xA0);
            SendData(0xA5);

            // Power optimization
            SendCommand(0xF8);
            SendData(0xA1);
            SendData(0x00);

            // Power optimization
            SendCommand(0xF8);
            SendData(0x73);
            SendData(0x41);

            SendCommand(0x16);      // PARTIAL_DISPLAY_REFRESH
            SendData(0x00);
            SendCommand(0x04);      // POWER_ON
            ReadBusy();

            SendCommand(0x00);      // PANEL_SETTING
            SendData(0xBF);         // KW-BF   KWR-AF    BWROTP 0f

            SendCommand(0x30);      // PLL_CONTROL
            SendData(0x90);         // 3A 100HZ   29 150Hz 39 200HZ    31 171HZ

            SendCommand(0X61);	    // RESOLUTION SETTING
            SendData(0x00);         // 176
            SendData(0xB0);
            SendData(0x01);         // 264
            SendData(0x08);

            SendCommand(0x82);      // VCM_DC_SETTING_REGISTER
            SendData(0x12);

            SendCommand(0X50);	    // VCOM AND DATA INTERVAL SETTING			
            SendData(0x57);
        }

        public void DisplayImage(Image<Rgba32> image)
        {
            byte[] buffer = GetBuffer(image);

            SendCommand(0X10);
            for (int i = 0; i < (int)(EPD_WIDTH * EPD_HEIGHT / 8); i++)
                SendData(0XFF);
            SendCommand(0X13);
            _EPCfg.DelayMs(2000);
            for (int i = 0; i < (int)(EPD_WIDTH * EPD_HEIGHT / 8); i++)
                SendData(buffer[i]);
            SendCommand(0X12);
            ReadBusy();
        }

        private byte[] GetBuffer(Image<Rgba32> image)
        {
            var buf = new byte[(int)(EPD_WIDTH / 8) * EPD_HEIGHT];
            Array.Fill(buf, (byte)0xFF);

            image.Mutate(x => x.BlackWhite());

            var imwidth = image.Width;
            var imheight = image.Height;

            if (imwidth == EPD_WIDTH && imheight == EPD_HEIGHT)
            {
                for (var y = 0; y < imheight; y++)
                {
                    for (var x = 0; x < imwidth; x++)
                    {
                        var pixel = image[x, y];
                        if (pixel.R == 0 && pixel.G == 0 && pixel.B == 0)
                        {
                            var b = 0x80 >> (x % 8);
                            b = ~b;
                            buf[(x + y * EPD_WIDTH) / 8] &= (byte)b;
                        }
                    }
                }
            }
            else if (imwidth == EPD_HEIGHT && imheight == EPD_WIDTH)
            {
                for (var y = 0; y < imheight; y++)
                {
                    for (var x = 0; x < imwidth; x++)
                    {
                        var newx = y;
                        var newy = EPD_HEIGHT - x - 1;
                        var pixel = image[x, y];
                        if (pixel.R == 0 && pixel.G == 0 && pixel.B == 0)
                        {
                            var b = 0x80 >> (y % 8);
                            b = ~b;
                            buf[(newx + newy * EPD_WIDTH) / 8] &= (byte)b;
                        }
                    }
                }
            }
            return buf;
        }
    }
}