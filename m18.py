
import serial
import time, struct, code
import argparse


class M18:
    SYNC_BYTE = 0xAA
    CAL_CMD = 0x55
    CONF_CMD = 0x60
    SNAP_CMD = 0x61
    KEEPALIVE_CMD = 0x62

    CUTOFF_CURRENT = 300
    MAX_CURRENT = 6000

    ACC = 4

    def __init__(self, port):
        self.port = serial.Serial(port, baudrate=2000, timeout=1, stopbits=1)

    def reset(self):
        self.port.baudrate = 100
        self.port.write(b'\x00')
        self.port.baudrate = 2000
        time.sleep(0.3)
        self.port.reset_input_buffer()
        self.port.write(b'\x55')
        response = self.port.read(1)
        if response == b'\x55':
            print("Received synchronisation byte")
            return True
        else:
            print(f"Unexpected response: {response}")
            return False
        
    def reverse_bits(self, byte):
        return int(f"{byte:08b}"[::-1], 2)
    
    def checksum(self, payload):
        checksum = 0
        for byte in payload:
            checksum += byte & 0xFFFF
        return checksum
    
    def send_command(self, lsb_command):
        self.port.reset_input_buffer()
        lsb_command += struct.pack(">H", self.checksum(lsb_command)) 
        msb_command = bytearray(self.reverse_bits(byte) for byte in lsb_command)
        debug_print = " ".join(f"{byte:02X}" for byte in lsb_command)
        print(f"Sending: {debug_print}")
        self.port.write(msb_command)

    def read_response(self, size):
        msb_response = self.port.read(size)
        lsb_response = bytearray(self.reverse_bits(byte) for byte in msb_response)
        debug_print = " ".join(f"{byte:02X}" for byte in lsb_response)
        print(f"Received: {debug_print}")
        time.sleep(0.1)
        return lsb_response

    def configure(self):
        print("sending conf")
        self.send_command(struct.pack('>BBBHHH', self.CONF_CMD, self.ACC, 8, 
                                    self.CUTOFF_CURRENT, self.MAX_CURRENT, self.MAX_CURRENT))
        return self.read_response(5)

    def get_snap(self):
        self.send_command(struct.pack('>BBB', self.SNAP_CMD, self.ACC, 0))
        return self.read_response(8)
    
    def keepalive(self):
        self.send_command(struct.pack('>BBB', self.KEEPALIVE_CMD, self.ACC, 0))
        return self.read_response(8)
    
    def calibrate(self):
        self.send_command(struct.pack('>BBB', self.CAL_CMD, self.ACC, 0))
        return self.read_response(8)
    
    def simulate(self):
        self.reset()
        time.sleep(2)
        self.configure()
        self.get_snap()
        while True:
            self.keepalive()
            time.sleep(0.3)



if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="M18 Protocol Interface")
    parser.add_argument('port', type=str, help="Serial port to connect to (e.g., COM5)")
    args = parser.parse_args()

    m = M18(args.port)

    if not m.reset():
        print('WARNING: Failed to initiate protocol')

    print("Will now go into shell mode. For there you can send commands such as: \n \
           m.reset() \n \
           m.get_snap() \n \
           m.configure() \n \
           m.calibrate() \n \
           m.keepalive() \n \
           m.simulate() \n")
    code.InteractiveConsole(locals = locals()).interact('Entering shell...')
