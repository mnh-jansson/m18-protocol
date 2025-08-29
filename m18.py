
import serial
import time, struct, code
import argparse
import datetime
import math
try:
    import readline
except ImportError:
    pass

data_matrix = [
    [0x00, 0x00, 0x02],
    [0x00, 0x02, 0x02],
    [0x00, 0x04, 0x05],
    [0x00, 0x0D, 0x04],
    [0x00, 0x11, 0x04],
    [0x00, 0x15, 0x04],
    [0x00, 0x19, 0x04],
    [0x00, 0x23, 0x14],
    [0x00, 0x37, 0x04],
    [0x00, 0x69, 0x02],
    [0x00, 0x7B, 0x01],
    [0x40, 0x00, 0x04],
    [0x40, 0x0A, 0x0A],
    [0x40, 0x14, 0x02],
    [0x40, 0x16, 0x02],
    [0x40, 0x19, 0x02], # New - 20 Aug
    [0x40, 0x1B, 0x02], 
    [0x40, 0x1D, 0x02], # New - 20 Aug
    [0x40, 0x1F, 0x02], # New - 20 Aug
    [0x60, 0x00, 0x02],
    [0x60, 0x02, 0x02],
    [0x60, 0x04, 0x04],
    [0x60, 0x08, 0x04],
    [0x60, 0x0C, 0x02], # New - 20 Aug
    [0x90, 0x00, 0x3A], # 338 byte RAM chunk. Grab every 58 bytes
    [0x90, 0x3A, 0x3A], # RAM chunk
    [0x90, 0x74, 0x3A], # RAM chunk
    [0x90, 0xAE, 0x3A], # RAM chunk
    [0x90, 0xE8, 0x3A], # RAM chunk
    [0x91, 0x22, 0x30], # RAM chunk. Grab last 48 bytes
    [0x91, 0x52, 0x00], # Always empty. Maybe marks end of RAM chunk
    [0xA0, 0x00, 0x06]
]


# label, addr, len, type 
#   uint - unsigned integer
#   date - UNIX time (seconds from 1 Jan 1970)
#   ascii - ascii string
#   sn - serial number (2 bytes battery type, 3 bytes serial)
#   adc_t - analog-to-digital converter temperature (mV of thermistor)
#   dec_t - decimal temperature (byte_1 + byte_2/255)
#   cell_v - cell voltages (1: 3568, 2: 3567, 3:3570, etc)
data_id = [
    [0x0000, 2,  "uint",  "Cell type"],                       # 0
    [0x0002, 2,  "uint",  "Unknown (always 0)"], 
    [0x0004, 5,  "sn",    "Capacity & Serial number (?)"], 
    [0x000D, 4,  "uint",  "Unknown (4th code?)"], 
    [0x0011, 4,  "date",  "Manufacture date"], 
    [0x0015, 4,  "date",  "Date of first charge (Forge)"], 
    [0x0019, 4,  "date",  "Date of last charge (Forge)"], 
    [0x0023, 20, "ascii", "Note (ascii string)"], 
    [0x0037, 4,  "date",  "Current date"], 
    [0x0069, 2,  "uint",  "Unknown (always 2)"]	, 
    [0x007B, 1,  "uint",  "Unknown (always 0)"],              # 10
    [0x4000, 4,  "uint",  "Unknown (Forge)"], 
    [0x400A, 10, "cell_v","Cell voltages (mV)"], 
    [0x4014, 2,  "adc_t", "Temperature (C) (non-Forge)"], 
    [0x4016, 2,  "uint",  "Unknown (Forge)"], 
    [0x4019, 2,  "uint",  "Unknown (Forge)"], 
    [0x401B, 2,  "uint",  "Unknown (Forge)"], 
    [0x401D, 2,  "uint",  "Unknown (Forge)"], 
    [0x401F, 2,  "dec_t", "Temperature (C) (Forge)"], 
    [0x6000, 2,  "uint",  "Unknown (Forge)"], 
    [0x6002, 2,  "uint",  "Unknown (Forge)"],                 # 20
    [0x6004, 4,  "uint",  "Unknown (Forge)"], 
    [0x6008, 4,  "uint",  "Unknown (Forge)"], 
    [0x600C, 2,  "uint",  "Unknown (Forge)"], 
    [0x9000, 4,  "date",  "Date of first charge (rounded)"], 
    [0x9004, 4,  "date",  "Date of last tool use (rounded)"], 
    [0x9008, 4,  "date",  "Date of last charge (rounded)"], 
    [0x900C, 4,  "date",  "Unknown date (often zero)"], 
    [0x9010, 2,  "uint",  "Days since first charge"], 
    [0x9012, 4,  "uint",  "Total discharge (amp-sec)"], 
    [0x9016, 4,  "uint",  "Total discharge (watt-sec or joules)"],    #30 
    [0x901A, 4,  "uint",  "Total charge count"], 
    [0x901E, 2,  "uint",  "Dumb charge count (J2>7.1V for >=0.48s)"], 
    [0x9020, 2,  "uint",  "Redlink (UART) charge count"], 
    [0x9022, 2,  "uint",  "Completed charge count (?)"], 
    [0x9024, 4,  "hhmmss","Total charging time (HH:MM:SS)"], 
    [0x9028, 4,  "hhmmss","Time on charger whilst full (HH:MM:SS)"], 
    [0x902C, 2,  "uint",  "Unknown (almost always 0)"], 
    [0x902E, 2,  "uint",  "Charge started with a cell < 2.5V"], 
    [0x9030, 2,  "uint",  "Unknown (discharge to empty?)"], 
    [0x9032, 2,  "uint",  "Num. overheat on tool (must be > 10A)"],   #40
    [0x9034, 2,  "uint",  "Unknown (overcurrent?)"], 
    [0x9036, 2,  "uint",  "Soft overloads (trigger low voltage)"], 
    [0x9038, 2,  "uint",  "Hard overloads (4 LEDs)"], 
    [0x903A, 2,  "uint",  "Discharge @ 10-20A (seconds)"], 
    [0x903C, 2,  "uint",  "          @ 20-30A (could be watts)"], 
    [0x903E, 2,  "uint",  "          @ 30-40A      "], 
    [0x9040, 2,  "uint",  "          @ 40-50A      "], 
    [0x9042, 2,  "uint",  "          @ 50-60A      "], 
    [0x9044, 2,  "uint",  "          @ 60-70A      "], 
    [0x9046, 2,  "uint",  "          @ 70-80A      "],    #50
    [0x9048, 2,  "uint",  "          @ 80-90A      "], 
    [0x904A, 2,  "uint",  "          @ 90-100A     "], 
    [0x904C, 2,  "uint",  "          @ 100-110A    "], 
    [0x904E, 2,  "uint",  "          @ 110-120A    "], 
    [0x9050, 2,  "uint",  "          @ 120-130A    "], 
    [0x9052, 2,  "uint",  "          @ 130-140A    "], 
    [0x9054, 2,  "uint",  "          @ 140-150A    "], 
    [0x9056, 2,  "uint",  "          @ 150-160A    "], 
    [0x9058, 2,  "uint",  "          @ 160-170A    "], 
    [0x905A, 2,  "uint",  "          @ 170-180A    "],    #60
    [0x905C, 2,  "uint",  "          @ 180-190A    "], 
    [0x905E, 2,  "uint",  "          @ 190-200A    "], 
    [0x9060, 2,  "uint",  "          @ 200-210A    "], 
    [0x9062, 2,  "uint",  "Unknown (larger in lower Ah packs)"], 
    [0x9064, 2,  "uint",  "Discharge @ 10-15A (seconds)"], 
    [0x9066, 2,  "uint",  "          @ 15-20A (could be watts)"], 
    [0x9068, 2,  "uint",  "          @ 20-25A      "], 
    [0x906A, 2,  "uint",  "          @ 25-30A      "], 
    [0x906C, 2,  "uint",  "          @ 30-35A      "], 
    [0x906E, 2,  "uint",  "          @ 35-40A      "],    #70
    [0x9070, 2,  "uint",  "          @ 40-45A      "], 
    [0x9072, 2,  "uint",  "          @ 45-50A      "], 
    [0x9074, 2,  "uint",  "          @ 50-55A      "], 
    [0x9076, 2,  "uint",  "          @ 55-60A      "], 
    [0x9078, 2,  "uint",  "          @ 60-65A      "], 
    [0x907A, 2,  "uint",  "          @ 65-70A      "], 
    [0x907C, 2,  "uint",  "          @ 70-65A      "], 
    [0x907E, 2,  "uint",  "          @ 75-80A      "], 
    [0x9080, 2,  "uint",  "          @ 80-85A      "], 
    [0x9082, 2,  "uint",  "          @ 85-90A      "],    #80
    [0x9084, 2,  "uint",  "          @ 90-95A      "], 
    [0x9086, 2,  "uint",  "          @ 95-100A     "], 
    [0x9088, 2,  "uint",  "          @ 100-105A    "], 
    [0x908A, 2,  "uint",  "          @ 105-110A    "], 
    [0x908C, 2,  "uint",  "          @ 110-115A    "], 
    [0x908E, 2,  "uint",  "          @ 115-120A    "], 
    [0x9090, 2,  "uint",  "          @ 120-125A    "], 
    [0x9092, 2,  "uint",  "          @ 125-130A    "], 
    [0x9094, 2,  "uint",  "          @ 130-135A    "], 
    [0x9096, 2,  "uint",  "          @ 135-140A    "],    #90
    [0x9098, 2,  "uint",  "          @ 140-145A    "], 
    [0x909A, 2,  "uint",  "          @ 145-150A    "], 
    [0x909C, 2,  "uint",  "          @ 150-155A    "], 
    [0x909E, 2,  "uint",  "          @ 155-160A    "], 
    [0x90A0, 2,  "uint",  "          @ 160-165A    "], 
    [0x90A2, 2,  "uint",  "          @ 165-170A    "], 
    [0x90A4, 2,  "uint",  "          @ 170-175A    "], 
    [0x90A6, 2,  "uint",  "          @ 175-180A    "], 
    [0x90A8, 2,  "uint",  "          @ 180-185A    "], 
    [0x90AA, 2,  "uint",  "          @ 185-190A    "],    #100
    [0x90AC, 2,  "uint",  "          @ 190-195A    "], 
    [0x90AE, 2,  "uint",  "          @ 195-200A    "], 
    [0x90B0, 2,  "uint",  "          @ 200A+       "], 
    [0x90B2, 2,  "uint",  "Charge started < 17V"], 
    [0x90B4, 2,  "uint",  "Charge started 17-18V"], 
    [0x90B6, 2,  "uint",  "Charge started 18-19V"], 
    [0x90B8, 2,  "uint",  "Charge started 19-20V"], 
    [0x90BA, 2,  "uint",  "Charge started 20V+"], 
    [0x90BC, 2,  "uint",  "Charge ended < 17V"], 
    [0x90BE, 2,  "uint",  "Charge ended 17-18V"],         #110
    [0x90C0, 2,  "uint",  "Charge ended 18-19V"],  
    [0x90C2, 2,  "uint",  "Charge ended 19-20V"],  
    [0x90C4, 2,  "uint",  "Charge ended 20V+"],
    [0x90C6, 2,  "uint",  "Charge start temp -30C to -20C"], 
    [0x90C8, 2,  "uint",  "Charge start temp -20C to -10C"], 
    [0x90CA, 2,  "uint",  "Charge start temp -10C to 0C"],  
    [0x90CC, 2,  "uint",  "Charge start temp 0C to +10C"],  
    [0x90CE, 2,  "uint",  "Charge start temp +10C to +20C"],  
    [0x90D0, 2,  "uint",  "Charge start temp +20C to +30C"], 
    [0x90D2, 2,  "uint",  "Charge start temp +30C to +40C"],      #120
    [0x90D4, 2,  "uint",  "Charge start temp +40C to +50C"],  
    [0x90D6, 2,  "uint",  "Charge start temp +50C to +60C"], 
    [0x90D8, 2,  "uint",  "Charge start temp +60C to +70C"],  
    [0x90DA, 2,  "uint", "Charge start temp +70C to +80C"],  
    [0x90DC, 2,  "uint",  "Charge start temp +80C and over"], 
    [0x90DE, 2,  "uint",  "Charge end temp -30C to -20C"], 
    [0x90E0, 2,  "uint",  "Charge end temp -20C to -10C"], 
    [0x90E2, 2,  "uint",  "Charge end temp -10C to 0C"],  
    [0x90E4, 2,  "uint",  "Charge end temp 0C to +10C"],  
    [0x90E6, 2,  "uint",  "Charge end temp +10C to +20C"],         #130
    [0x90E8, 2,  "uint",  "Charge end temp +30C to +30C"],
    [0x90EA, 2,  "uint",  "Charge end temp +30C to +40C"],
    [0x90EC, 2,  "uint",  "Charge end temp +40C to +50C"],
    [0x90EE, 2,  "uint",  "Charge end temp +50C to +60C"],
    [0x90F0, 2,  "uint",  "Charge end temp +60C to +70C"],
    [0x90F2, 2,  "uint",  "Charge end temp +70C to +80C"],
    [0x90F4, 2,  "uint",  "Charge end temp +80C and over"], 
    [0x90F6, 2,  "uint",  "Dumb charge time (00:00-14:33)"], 
    [0x90F8, 2,  "uint",  "Dumb charge time (14:34-29:07)"], 
    [0x90FA, 2,  "uint",  "Dumb charge time (29:08-43:41)"],  #140
    [0x90FC, 2,  "uint",  "Dumb charge time (43:42-58:15)"], 
    [0x90FE, 2,  "uint",  "Dumb charge time (58:16-1:12:49)"], 
    [0x9100, 2,  "uint",  "Dumb charge time (1:12:50-1:27:23)"], 
    [0x9102, 2,  "uint",  "Dumb charge time (1:27:24-1:41:57)"], 
    [0x9104, 2,  "uint",  "Dumb charge time (1:41:58-1:56:31)"], 
    [0x9106, 2,  "uint",  "Dumb charge time (1:56:32-2:11:05)"], 
    [0x9108, 2,  "uint",  "Dumb charge time (2:11:06-2:25:39)"], 
    [0x910A, 2,  "uint",  "Dumb charge time (2:25:40-2:40:13)"], 
    [0x910C, 2,  "uint",  "Dumb charge time (2:40:14-2:54:47)"], 
    [0x910E, 2,  "uint",  "Dumb charge time (2:54:48-3:09:21)"],             #150
    [0x9110, 2,  "uint",  "Dumb charge time (3:09:22-3:23:55)"], 
    [0x9112, 2,  "uint",  "Redlink charge time (00:00-17:03)"], 
    [0x9114, 2,  "uint",  "Redlink charge time (17:04-34:07)"], 
    [0x9116, 2,  "uint",  "Redlink charge time (34:08-51:11)"], 
    [0x9118, 2,  "uint",  "Redlink charge time (51:12-1:08:15)"], 
    [0x911A, 2,  "uint",  "Redlink charge time (1:08:16-1:25:19)"], 
    [0x911C, 2,  "uint",  "Redlink charge time (1:25:20-1:42:23)"], 
    [0x911E, 2,  "uint",  "Redlink charge time (1:42:24-1:59:27)"], 
    [0x9120, 2,  "uint",  "Redlink charge time (1:59:28-2:16:31)"], 
    [0x9122, 2,  "uint",  "Redlink charge time (2:16:32-2:33:35)"],   #160 
    [0x9124, 2,  "uint",  "Redlink charge time (2:33:36-2:50:39)"], 
    [0x9126, 2,  "uint",  "Redlink charge time (2:50:40-3:07:43)"], 
    [0x9128, 2,  "uint",  "Redlink charge time (3:07:44-3:24:47)"], 
    [0x912A, 2,  "uint",  "Redlink charge time (3:24:48-3:41:51)"], 
    [0x912C, 2,  "uint",  "Redlink charge time (3:41:52-3:58:55)"], 
    [0x912E, 2,  "uint",  "Completed charge (?)"], 
    [0x9130, 2,  "uint",  "Unknown"], 
    [0x9132, 2,  "uint",  "Unknown"], 
    [0x9134, 2,  "uint",  "Unknown"], 
    [0x9136, 2,  "uint",  "Unknown"],       #170
    [0x9138, 2,  "uint",  "Unknown"], 
    [0x913A, 2,  "uint",  "Unknown"], 
    [0x913C, 2,  "uint",  "Unknown"], 
    [0x913E, 2,  "uint",  "Unknown"], 
    [0x9140, 2,  "uint",  "Unknown"], 
    [0x9142, 2,  "uint",  "Unknown"], 
    [0x9144, 2,  "uint",  "Unknown"], 
    [0x9146, 2,  "uint",  "Unknown"], 
    [0x9148, 2,  "uint",  "Unknown (days of use?)"], 
    [0x914A, 2,  "uint",  "Unknown"],       # 180
    [0x914C, 2,  "uint",  "Unknown"], 
    [0x914E, 2,  "uint",  "Unknown"], 
    [0x9150, 2,  "uint",  "Unknown"],       #183
]






def print_debug_bytes(data):
    data_print = " ".join(f"{byte:02X}" for byte in data)
    print(f"DEBUG: ", data_print)

class M18:
    SYNC_BYTE     = 0xAA
    CAL_CMD       = 0x55
    CONF_CMD      = 0x60
    SNAP_CMD      = 0x61
    KEEPALIVE_CMD = 0x62

    CUTOFF_CURRENT = 300
    MAX_CURRENT = 6000

    ACC = 4
    
    PRINT_TX = False
    PRINT_RX = False
    
    # Used to temporarily disable then restore print_tx/rx state
    PRINT_TX_SAVE = False 
    PRINT_RX_SAVE = False
        
    def txrx_print(self, enable = True):
        self.PRINT_TX = enable
        self.PRINT_RX = enable
        
    def txrx_save_and_set(self, enable = True):
        self.PRINT_TX_SAVE = self.PRINT_TX
        self.PRINT_RX_SAVE = self.PRINT_RX
        self.txrx_print(enable)
        
    def txrx_restore(self):
        self.PRINT_TX = self.PRINT_TX_SAVE
        self.PRINT_RX = self.PRINT_RX_SAVE
            

    def __init__(self, port):
        self.port = serial.Serial(port, baudrate=4800, timeout=0.8, stopbits=2)
        self.idle()

    def reset(self):
        self.ACC = 4
        self.port.break_condition = True
        self.port.dtr = True
        time.sleep(0.3)
        self.port.break_condition = False
        self.port.dtr = False
        time.sleep(0.3)
        self.send(struct.pack('>B', self.SYNC_BYTE))
        try:
            response = self.read_response(1)
        except:
            return False
        time.sleep(0.01)
        if response and response[0] == self.SYNC_BYTE:
            return True
        else:
            print(f"Unexpected response: {response}")
            return False

    def update_acc(self):
        acc_values = [0x04, 0x0C, 0x1C]
        current_index = acc_values.index(self.ACC)
        next_index = (current_index + 1) % len(acc_values)
        self.ACC = acc_values[next_index]

    def reverse_bits(self, byte):
        return int(f"{byte:08b}"[::-1], 2)
    
    def checksum(self, payload):
        checksum = 0
        for byte in payload:
            checksum += byte & 0xFFFF
        return checksum

    def add_checksum(self, lsb_command):
        lsb_command += struct.pack(">H", self.checksum(lsb_command)) 
        return lsb_command
    
    def send(self, command):
        self.port.reset_input_buffer()
        debug_print = " ".join(f"{byte:02X}" for byte in command)
        msb = bytearray(self.reverse_bits(byte) for byte in command)
        if self.PRINT_TX:
            print(f"Sending:  {debug_print}")
        self.port.write(msb)
    
    def send_command(self, command):
        self.send(self.add_checksum(command))

    def read_response(self, size):
        msb_response = self.port.read(1)
        if not msb_response or len(msb_response) < 1:
            raise ValueError("Empty response")
        if self.reverse_bits(msb_response[0]) == 0x82:
            msb_response += self.port.read(1)
        else:
            msb_response += self.port.read(size-1)
        lsb_response = bytearray(self.reverse_bits(byte) for byte in msb_response)
        debug_print = " ".join(f"{byte:02X}" for byte in lsb_response)
        if self.PRINT_RX:
            print(f"Received: {debug_print}")
        return lsb_response

    def configure(self, state):
        self.ACC = 4
        self.send_command(struct.pack('>BBBHHHBB', self.CONF_CMD, self.ACC, 8, 
                                    self.CUTOFF_CURRENT, self.MAX_CURRENT, self.MAX_CURRENT, state, 13))
        return self.read_response(5)

    def get_snapchat(self):
        self.send_command(struct.pack('>BBB', self.SNAP_CMD, self.ACC, 0))
        self.update_acc()
        return self.read_response(8)
    
    def keepalive(self):
        self.send_command(struct.pack('>BBB', self.KEEPALIVE_CMD, self.ACC, 0))
        return self.read_response(9)
    
    def calibrate(self):
        self.send_command(struct.pack('>BBB', self.CAL_CMD, self.ACC, 0))
        self.update_acc()
        return self.read_response(8)
    
    def simulate(self):
        print("Simulating charger communication")
        
        self.txrx_save_and_set(True) # Turn on TX/RX messages
        
        self.reset()
        #print_debug_bytes(self.configure(2))
        #print_debug_bytes(self.get_snapchat())
        #time.sleep(0.6)
        #print_debug_bytes(self.keepalive())
        #print_debug_bytes(self.configure(1))
        #print_debug_bytes(self.get_snapchat())
        
        tmp = self.configure(2)
        tmp = self.get_snapchat()
        time.sleep(0.6)
        tmp = self.keepalive()
        tmp = self.configure(1)
        tmp = self.get_snapchat()
        try:
            while True:
                time.sleep(0.5)
                # print_debug_bytes(self.keepalive())
                tmp = self.keepalive()
        except KeyboardInterrupt:
            print("\nSimulation aborted by user. Exiting gracefully...")
        finally:
            self.idle() 
            
        self.txrx_restore() # restore TX/RX print status
    

    def simulate_for(self, duration):
        # Simulate charging for 'time' seconds
        print(f"Simulating charger communication for {duration} seconds...")
        begin_time = time.time()
        self.reset()
        self.configure(2)
        self.get_snapchat()
        time.sleep(0.6)
        self.keepalive()
        self.configure(1)
        self.get_snapchat()
        try:
            #start_time = time.time()
            while (time.time() - begin_time) < duration:
                time.sleep(0.5)
                self.keepalive()
        except KeyboardInterrupt:
            print("\nSimulation aborted by user. Exiting gracefully...")
        finally:
            self.idle() 
            print(f"Duration: ", time.time() - begin_time)

    def debug(self, a,b,c,length):
        
        # Turn off debug, restore after printing
        rx_debug = self.PRINT_RX
        tx_debug = self.PRINT_TX
        self.PRINT_TX = False
        self.PRINT_RX = False
        
        self.reset()
        self.PRINT_TX = tx_debug
        data = self.cmd(a,b,c,length)
        data_print = " ".join(f"{byte:02X}" for byte in data)
        print(f"Response from: 0x{(a * 0x100 + b):04X}:", data_print)
        self.idle()
        self.PRINT_RX = rx_debug
        
    def try_cmd(self, cmd, msb, lsb, len, ret_len=0 ):
        # Turn off TX/RX printing, restore after printing
        self.txrx_save_and_set(False)
        
        # default is read 5 bytes more than payload (3-byte header, 2-byte cksum)
        if ( ret_len == 0 ):
            ret_len = len + 5
        
        self.reset()
        self.send_command(struct.pack('>BBBBBB', cmd, 0x04, 0x03, msb, lsb, len))
        data = self.read_response(ret_len)
        data_print = " ".join(f"{byte:02X}" for byte in data)
        print(f"Response from: 0x{(msb * 0x100 + lsb):04X}:", data_print)
        self.idle()
        self.txrx_restore()
        
    
    def cmd(self, a,b,c,length, command = 0x01):
        self.send_command(struct.pack('>BBBBBB', command, 0x04, 0x03, a, b, c))
        return self.read_response(length)
        

    def brute(self, a, b, len = 0xFF, command = 0x01):
        self.reset()
        try:
            for i in range(len):
                ret = self.cmd(a, b, i, i+5, command)
                if ret[0] == 0x81:
                    data_print = " ".join(f"{byte:02X}" for byte in ret)
                    print(f"Valid response from: 0x{(a * 0x100 + b):04X} with length: 0x{i:02X}:", data_print)
        except KeyboardInterrupt:
            print("\nSimulation aborted by user. Exiting gracefully...")
        finally:
            self.idle() 

    def full_brute(self, start=0, stop=0xFFFF, len = 0xFF):        
    # query every address by calling 'brute()' from 'start' to 'stop'
    # 'len' should be 0x01 to 0xFF. 0x0A is a good value that should find all registers
        try:
            for addr in range(start, stop): 
                msb = (addr >> 8) & 0xFF # separate upper 8-bits of addr
                lsb = addr & 0xFF # separate lower 8-bits of addr
                self.brute(msb,lsb, len, 0x01)
                if ( (addr % 256) == 0 ):
                    print(f"addr = 0x{addr:04X} ", datetime.datetime.now() )
        except KeyboardInterrupt:
            print("\nSimulation aborted by user. Exiting gracefully...")
            print(f"\nStopped at address: 0x{addr:04X}")
        finally:
            self.idle() 
    
    def wcmd(self, a,b,c,length):
        self.send_command(struct.pack('>BBBBBB', 0x01, 0x05, 0x03, a, b, c))
        return self.read_response(length)
    
    def wdebug(self, a,b,c,length):
        self.reset()
        data = self.wcmd(a,b,c,length)
        data_print = " ".join(f"{byte:02X}" for byte in data)
        print(f"Response from: 0x{(a * 0x100 + b):04X}:", data_print)
        self.idle()
    
    def wbrute(self, start=0):
        upper = start >> 8 & 0xff
        lower = start & 0xff
        self.reset()
        try:
            for a in range(upper, 0xff): 
                for b in range(lower, 0xff): 
                    ret = self.wcmd(a, b, 2, 2+5)
                    if ret[0] == 0x80:
                        data_print = " ".join(f"{byte:02X}" for byte in ret)
                        print(f"Valid write at: 0x{(a * 0x100 + b):04X}: ", data_print)
        except KeyboardInterrupt:
            print("\nSimulation aborted by user. Exiting gracefully...")
        finally:
            self.idle() 

    def write_message(self, message):
        try:
            if len(message) > 0x14:
                print("ERROR: Message too long!")
                return
            print(f"Writing \"{message}\" to memory")
            self.reset()
            message = message.ljust(0x14, '-')
            for i, char in enumerate(message):
                self.wcmd(0,0x23+i,ord(char), 2)
        except Exception as e:
            print(f"write_message: Failed with error: {e}")

    def idle(self):
        self.port.break_condition = True
        self.port.dtr = True

    def high(self):
        self.port.break_condition = False
        self.port.dtr = False
        
    def high_for(self, duration):
        self.high()
        time.sleep(duration)
        self.idle()

    def calculate_temperature(self, adc_value):
        R1 = 10e3  # 10k ohm
        R2 = 20e3  # 20k ohm
        T1 = 50    # 50°C
        T2 = 35    # 35°C

        adc1 = 0x0180
        adc2 = 0x022E
        
        m = (T2 - T1) / (R2 - R1)
        b = T1 - m * R1

        resistance = R1 + (adc_value - adc1) * (R2 - R1) / (adc2 - adc1)
        temperature = m * resistance + b

        return round(temperature, 2)

    def read_clock(self):
        try:
            while True:
                self.reset()
                time_data = self.cmd(0x00, 0x37, 0x4, (0x4 + 5))
                dt = self.bytes2dt(time_data[3:7])
                print(f"Internal time: {dt.strftime('%Y-%m-%d %H:%M:%S')}")
                time.sleep(1)
        except KeyboardInterrupt:
            print("\nRead aborted by user. Exiting gracefully...")
        except Exception as e:
            print(f"read_clock: Failed with error: {e}")
        finally:
            self.idle() 

    def bytes2dt(self, time_bytes):
        epoch_time = int.from_bytes(time_bytes, 'big')
        dt = datetime.datetime.fromtimestamp(epoch_time, tz=datetime.UTC)
        return dt

    def read_bat(self):
        
        # turn off debugging messages
        self.txrx_save_and_set(False)
        
        try:
            self.reset()
            voltage_data = self.cmd(0x40, 0x0a, 0x0a, (0x0a + 5)) # read all voltages
            temp_data = self.cmd(0x40, 0x14, 0x2, (0x2 + 5))
            ram_data = self.cmd(0x90, 0x0a, 0x3b, (0x3b + 5))
            mfg_time_data = self.cmd(0x00, 0x11, 0x4, (0x4 + 5))
            now_data = self.cmd(0x00, 0x37, 0x4, (0x4 + 5))
            bytes_message = self.cmd(0x00, 0x23, 0x14, (0x14 + 5))
            self.idle()

            voltage_data = voltage_data[3:]

            cell_voltages = [int.from_bytes(voltage_data[i:i+2], 'big') / 1000 for i in range(0, 10, 2)]
            for i, voltage in enumerate(cell_voltages, start=1):
                print(f"Cell {i}: {voltage:.3f}V")

            days = (ram_data[9]<<8) + ram_data[10]
            num_charges = (ram_data[21]<<8) + ram_data[22]
            temp_adc = int.from_bytes(temp_data[3:5], 'big')
            temp = self.calculate_temperature(temp_adc)
            mfg_dt = self.bytes2dt(mfg_time_data[3:7])
            now_dt = self.bytes2dt(now_data[3:7])
            string = bytes_message[3:23].decode('utf-8')

            print(f"\nCell temperature: {temp}°C")
            print(f"Number of charges: {num_charges}")
            print(f"Internal clock: {now_dt.strftime('%Y-%m-%d %H:%M:%S')}")
            print(f"Days since first charge: {days}")
            print(f"Date of manufacturing: {mfg_dt.strftime('%Y-%m-%d %H:%M:%S')}")
            print(f"Message: {string}")
        except Exception as e:
            print(f"read_bat: Failed with error: {e}")
            
        # restore debug status
        self.txrx_restore()
        

    def read_all(self):
        try:
            self.reset()
            for addr_h, addr_l, length in data_matrix:
                response = self.cmd(addr_h, addr_l, length, (length + 5))
                if response and len(response) >= 4 and response[0] == 0x81:
                    data = response[3:3 + length]
                    data_print = " ".join(f"{byte:02X}" for byte in data)
                    print(f"Response from: 0x{(addr_h * 0x100 + addr_l):04X}:", data_print)
                else:
                    data_print = " ".join(f"{byte:02X}" for byte in response)
                    print(f"Invalid response from: 0x{(addr_h * 0x100 + addr_l):04X} Response: {data_print}")
            self.idle()
        except Exception as e:
            print(f"read_all: Failed with error: {e}")
    

    # read data by ID. Default is print all
    def read_id(self, id_array = [] ):
        # If empty, default is print all
        if ( len(id_array) == 0 ):
            id_array = range(0,len(data_id))
        
        try:
            self.reset()
            # Do dummy read to update 0x9000 data
            for addr_h, addr_l, length in data_matrix:
                response = self.cmd(addr_h, addr_l, length, (length + 5))
            self.idle()
            time.sleep(0.1)
            
            # Add date to top
            now = datetime.datetime.now()
            formatted_time = now.strftime("%Y-%m-%d %H:%M:%S")
            print(formatted_time)            
            print("ID  ADDR   LEN TYPE       LABEL                                   VALUE")
            
            self.reset()
            for i in id_array:
                addr = data_id[i][0]
                addr_h = (addr >> 8) & 0xFF # separate upper 8-bits of addr
                addr_l = addr & 0xFF # separate lower 8-bits of addr
                length = data_id[i][1]
                type = data_id[i][2]
                label = data_id[i][3]

                response = self.cmd(addr_h, addr_l, length, (length + 5))
                if response and len(response) >= 4 and response[0] == 0x81:
                    # extract payload. message without header and cksum
                    data = response[3:(3+length)]
                                        
                    # process data according to type
                    # (uint, date, ascii, sn, adc_t, dec_t, cell_v)
                    match type:
                        case "uint":
                            value = int.from_bytes(data, 'big')
                        case "date":
                            date_value = self.bytes2dt(data)
                            value = date_value.strftime('%Y-%m-%d %H:%M:%S')
                        case "hhmmss":
                            dur = int.from_bytes(data, 'big')
                            td = time.gmtime(dur)
                            value = time.strftime("%H:%M:%S", td)
                        case "ascii":
                            str = data.decode('utf-8')
                            value = f'\"{str}\"'
                        case "sn":
                            btype = int.from_bytes(data[0:2],'big')
                            serial = int.from_bytes(data[2:5],'big')
                            value = f"Type: {btype:3d}, Serial: {serial:d}"
                        case "adc_t":
                            value = self.calculate_temperature(int.from_bytes(data, 'big'))
                        case "dec_t":
                            value = int.from_bytes(data[0], 'big') + int.from_bytes(data[1], 'big')/255
                        case "cell_v":
                            cv = [int.from_bytes(data[i:i+2], 'big') for i in range(0, 10, 2)]
                            value = f"1: {cv[0]:4d}, 2: {cv[1]:4d}, 3: {cv[2]:4d}, 4: {cv[3]:4d}, 5: {cv[4]:4d}"
                   
                else:
                    value = "------"
                
                # Print formatted data
                print(f"{i:3d} 0x{addr:04X} {length:2d} {type:>6}   {label:<39} {value:<}")
            
        except Excerption as e:
            print(f"read_id: Failed with error: {e}")

    
    def read_all_spreadsheet(self):
        try:
            self.reset()
            
            # Do dummy read to update 0x9000 data
            for addr_h, addr_l, length in data_matrix:
                response = self.cmd(addr_h, addr_l, length, (length + 5))
            self.idle()
            time.sleep(0.5)
            
            self.reset()
            
            # Add date to top
            now = datetime.datetime.now()
            formatted_time = now.strftime("%Y-%m-%d %H:%M:%S")
            print(formatted_time)
            
            for addr_h, addr_l, length in data_matrix:
                response = self.cmd(addr_h, addr_l, length, (length + 5))
                if response and len(response) >= 4 and response[0] == 0x81:
                    # extract payload. message without header and cksum
                    data = response[3:(3+length)]
                    print(f"0x{(addr_h * 0x100 + addr_l):04X}")
                    if len(data) == 0:
                        print("EMPTY")
                    else:
                        # data_print = "\n".join(f"{byte:02X}" for byte in data)
                        data_print = "\n".join(f"{byte}" for byte in data)
                        print(data_print)
                else:
                    print(f"0x{(addr_h * 0x100 + addr_l):04X}")
                    data_print = " ".join(f"{byte:02X}" for byte in response)
                    print(f"INV: {data_print}")
                    # pad with "blank" so data lines up in spreadsheet
                    for i in range(1,length): 
                        print("blank")
                    
            self.idle()
        except Exception as e:
            print(f"read_all_spreadsheet: Failed with error: {e}")

    def help(self):
        print("Functions: \n \
           DIAGNOSTICS: \n \
           m.read_id() - print labelled and formatted diagnostics \n \
           m.read_bat() - formatted print of all mapped registers\n \
           m.read_all() - print all known registers in 0x01 command \n \
           m.read_all_spreadsheet() - print registers in spreadsheet format \n \
           \n \
           CHARGING SIMULATION: \n \
           m.simulate() - simulate charging comms \n \
           m.simulate_for(t) - simulate for t seconds \n \
           m.high_for(t) - bring J2 high for t sec, then idle \n \
           \n \
           m.write_message(message) - write ascii string to 0x0023 register (20 chars)\n \
           \n \
           Debug: \n \
           m.PRINT_TX = True - boolean to enable TX messages \n \
           m.PRINT_RX = True - boolean to enable RX messages \n \
           m.txrx_print(bool) - set PRINT_TX & RX to bool \n \
           m.txrx_save_and_set(bool) - save PRINT_TX & RX state, then set both to bool \n \
           m.txrx_restore() - restore PRINT_TX & RX to saved values \n \
           m.brute(addr_msb, addr_lsb) \n \
           m.full_brute(start, stop, len) - check registers from 'start' to 'stop'. look for 'len' bytes \n \
           m.debug(addr_msb, addr_lsb, len, rsp_len) - send reset() then cmd() to battery \n \
           m.try_cmd(cmd, addr_h, addr_l, len) - try 'cmd' at [addr_h addr_l] with 'len' bytes \n \
           \n \
           Internal:\n \
           m.high() - bring J2 pin high (20V)\n \
           m.idle() - pull J2 pin low (0V) \n \
           m.reset() - send 0xAA to battery. Return true if batter yreplies wih 0xAA \n \
           m.get_snap() - request 'snapchat' from battery (0x61)\n \
           m.configure() - send 'configure' message (0x60, charger parameters)\n \
           m.calibrate() - calibration/interrupt command (0x55) \n \
           m.keepalive() - send charge current request (0x62) \n \
           \n \
           m.help() - this message\n") 


if __name__ == '__main__':
    parser = argparse.ArgumentParser(
        description="M18 Protocol Interface",
        epilog="Connect UART-TX to M18-J2 and UART-RX to M18-J1 to fake the charger and UART-GND to M18-GND")
    parser.add_argument('--port', type=str, help="Serial port to connect to (e.g., COM5)", default = "/dev/ttyUSB0")
    args = parser.parse_args()

    m = M18(args.port)
    
    m.help()
    
    code.InteractiveConsole(locals = locals()).interact('Entering shell...')
