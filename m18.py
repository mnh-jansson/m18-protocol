
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
    [0x90, 0x00, 0x3B],
    [0x90, 0x3B, 0x3B], # New - 20 Aug
    [0x90, 0x76, 0x3B], # New - 20 Aug
    [0x90, 0xB1, 0x3B], # New - 20 Aug
    [0x90, 0xEC, 0x3B], # New - 20 Aug
    [0x91, 0x27, 0x2B], # New - 20 Aug
    [0x91, 0x52, 0x00],
    [0xA0, 0x00, 0x06]
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
        self.reset()
        print_debug_bytes(self.configure(2))
        print_debug_bytes(self.get_snapchat())
        time.sleep(0.6)
        print_debug_bytes(self.keepalive())
        print_debug_bytes(self.configure(1))
        print_debug_bytes(self.get_snapchat())
        try:
            while True:
                time.sleep(0.5)
                print_debug_bytes(self.keepalive())
        except KeyboardInterrupt:
            print("\nSimulation aborted by user. Exiting gracefully...")
        finally:
            self.idle() 
    

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
            start_time = time.time()
            while (time.time() - start_time) < duration:
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
            
    def read_all_spreadsheet(self):
        try:
            self.reset()
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
            self.idle()
        except Exception as e:
            print(f"read_all_spreadsheet: Failed with error: {e}")

    def help(self):
        print("Functions: \n \
           m.simulate() - simulate charging comms \n \
           m.simulate_for(t) - simulate for t seconds \n \
           m.high_for(t) - bring J2 high for t sec, then idle \n \
           m.read_all() - print all known registers in 0x01 command \n \
           m.read_all_spreadsheet() - print registers in spreadsheet format \n \
           m.read_bat() - formatted print of all mapped registers\n \
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
