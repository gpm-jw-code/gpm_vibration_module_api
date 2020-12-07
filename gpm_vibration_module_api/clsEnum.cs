using System;
using System.Collections.Generic;
using System.Text;

namespace gpm_vibration_module_api
{
    public  class clsEnum
    {
        internal enum ControllerCommand
        {
            READVALUE, READSTVAL, BULKVALUE, BULKBREAK
        }
        public struct Module_Setting_Enum
        {
            public enum SENSOR_TYPE
            {
                High = 2, Genernal = 1
            }

            public enum DATA_LENGTH
            {
                none = 0, x1 = 512, x2 = 1024, x4 = 2048, x8 = 4096, x16 = 8192, Others
            }

            public enum ODR
            {
                _9F = 159, _87 = 135
            }

            public enum MEASURE_RANGE
            {
                MR_2G = 16384,
                MR_4G = 8192,
                MR_8G = 4096,
                MR_16G = 2048,
            }
        }
        /// <summary>
        /// GPM專用韌體ENUM
        /// </summary>
        internal struct FWSetting_Enum
        {
            internal enum ACC_CONVERT_ALGRIUM
            {
                Old, New, Bulk
            }

        }



        internal struct KeyPro
        {
            internal enum RY4CMD : ushort
            {
                RY_FIND = 1,            //Find Ry4S
                RY_FIND_NEXT,       //Find next
                RY_OPEN,            //Open Ry4S
                RY_CLOSE,           //Close Ry4S
                RY_READ,            //Read Ry4S
                RY_WRITE,           //Write Ry4S
                RY_RANDOM,          //Generate random
                RY_SEED,            //Generate seed
                RY_SET_MODULE = 11,
                RY_READ_USERID = 10,    //Read UID
                RY_CHECK_MODULE = 12,   //Check Module
                RY_WRITE_ARITHMETIC,//Write
                RY_CALCULATE1 = 14, //Calculate1
                RY_CALCULATE2,      //Calculate1
                RY_CALCULATE3,      //Calculate1
            };
            internal enum RY4_ERROR_CODE : uint
            {
                ERR_SUCCESS = 0,                            //No error
                ERR_NO_PARALLEL_PORT = 0x80300001,      //(0x80300001)No parallel port
                ERR_NO_DRIVER,                          //(0x80300002)No drive
                ERR_NO_ROCKEY,                          //(0x80300003)No Ry4S
                ERR_INVALID_PASSWORD,                   //(0x80300004)Invalid password
                ERR_INVALID_PASSWORD_OR_ID,             //(0x80300005)Invalid password or ID
                ERR_SETID,                              //(0x80300006)Set id error
                ERR_INVALID_ADDR_OR_SIZE,               //(0x80300007)Invalid address or size
                ERR_UNKNOWN_COMMAND,                    //(0x80300008)Unkown command
                ERR_NOTBELEVEL3,                        //(0x80300009)Inner error
                ERR_READ,                               //(0x8030000A)Read error
                ERR_WRITE,                              //(0x8030000B)Write error
                ERR_RANDOM,                             //(0x8030000C)Generate random error
                ERR_SEED,                               //(0x8030000D)Generate seed error
                ERR_CALCULATE,                          //(0x8030000E)Calculate error
                ERR_NO_OPEN,                            //(0x8030000F)The Ry4S is not opened
                ERR_OPEN_OVERFLOW,                      //(0x80300010)Open Ry4S too more(>16)
                ERR_NOMORE,                             //(0x80300011)No more Ry4S
                ERR_NEED_FIND,                          //(0x80300012)Need Find before FindNext
                ERR_DECREASE,                           //(0x80300013)Dcrease error
                ERR_AR_BADCOMMAND,                      //(0x80300014)Band command
                ERR_AR_UNKNOWN_OPCODE,                  //(0x80300015)Unkown op code
                ERR_AR_WRONGBEGIN,                      //(0x80300016)There could not be constant in first instruction in arithmetic 
                ERR_AR_WRONG_END,                       //(0x80300017)There could not be constant in last instruction in arithmetic 
                ERR_AR_VALUEOVERFLOW,                   //(0x80300018)The constant in arithmetic overflow
                ERR_UNKNOWN = 0x8030ffff,                   //(0x8030FFFF)Unkown error

                ERR_RECEIVE_NULL = 0x80300100,          //(0x80300100)Receive null
                ERR_PRNPORT_BUSY = 0x80300101               //(0x80300101)Parallel port busy

            };
            public enum KEYPRO_EXIST_STATE
            {
                Exist, NoInsert
            }
        }

    }
}
