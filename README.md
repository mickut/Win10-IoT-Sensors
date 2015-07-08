# Win10-IoT-Sensors
Windows 10 IoTCore sensor library for various I2C sensors.

Implemented sensors:
 - Rohm BH1750FVI ambient light sensor
 - Bosch BMP180 barometric sensor
 - Dorji DSTH01 relative humidity sensor
 - Honeywell HMC5883L digital 3-axis compass/magnetometer
 
These sensors show few of the different ways I2C based devices are made:

The BH1750FVI is based on 8-bit operation codes and you must use explicit write and read operations. It also has an ADDR pin that can be pulled low or high to change between two distint slave addresses. This enable having two separate sensors without reserving any of the host IO lines.

The BMP180, DSTH01 and HMC5583L are based on register access, with specific bits of registers to start an operation. The register based devices typically interpret the first written byte as an address pointer. Any subsequent bytes written are placed to current address and the address pointer gets incremented. For reading the operation is similar, and the address pointer can be pointed to a new place with a single byte write.

The DSTH01 has a Chip Select pin which must be pulled down to power up the device, and released when the device is not actively communicated with. This enables one to have multiple chips with same address at the cost of host IO-pins.

The HMC5583L provides a separate output to inform data availability. This eliminates the need for polling status registers on timers at a cost of an IO-line. It is most useful for a continous measurement mode, where the sensor notifies the host whenever a new measurement has been stored to the data registers.

Datasheets:

 - http://rohmfs.rohm.com/en/products/databook/datasheet/ic/sensor/light/bh1750fvi-e.pdf
 - http://www51.honeywell.com/aero/common/documents/myaerospacecatalog-documents/Defense_Brochures-documents/HMC5883L_3-Axis_Digital_Compass_IC.pdf
 - http://www.dorji.com/docs/data/DSTH01.pdf
 - https://ae-bst.resource.bosch.com/media/downloads/pressure/bmp180/Flyer_BMP180_08_2013_web.pdf
