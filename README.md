# ClipDataEx

ClipDataEx is a tool to exfiltrate data via the clipboard
on systems where regular file transfer is disabled.

It serves as a proof of concept, but is fully functional.
It may lack a few non-essential features
like a progress indicator on the sender side, and mesh network support.

## CAUTION!

**As mentioned above, this is a proof of concept.**

The application may not properly check inbound data for protocol conformation,
as such, do not use this application if you have potentially untrusted peers.

Being written in C# it should be safe
from most attacks that plague unmanaged languages.

In general, it's assumed that you control all clients that participate.

## Short Description

ClipDataEx can send files between any two machines on the world
that share the same clipboard and allow images to be exchanged.

This adds file sharing capabilities to virtually any screen sharing technology,
including but not limited to VNC, RDP, Citrix,
but also virtual machines such as VMWare and VirtualBox,
provided clipboard sharing is enabled.

Data is protected using encryption and integrity functions
to prevent people from peeking at the data or modifying it.

Note that ClipDataEx uses image data to transfer files.
You could use strings instead,
but be aware that you have to come up with a completely different encoding scheme,
mostly because string data cannot contain nullbytes.
You can use Base64 but this will increase data size a lot.

## Detailed Description

ClipDataEx sends files by encoding them inside of the pixel data of images.
The images are created in memory as needed.

Note: ClipDataEx does not perform steganography.
The images exclusively contain the transmitted data.
Because of the encryption, this makes the pixels appear completely random,
and made up of white noise.

### How Files are Transferred

A file is split up into chunks of data.
ClipDataEx has a hardcoded size of approximately 1 MB
to accomodate progress reporting on slow connections.
The theoretical limit per chunk is `0xFFFFFF` (16'777'215) bytes.
This would result in an image of approximately 2365x2365 pixels.
With 1 MB, the images are 592x592 pixels, which is a more reasonable size.

Note: Within the permitted size limits, chunks can be of any size.
ClipDataEx doesn't has this feature built-in,
but a user might decide to randomize the chunk size for every sent chunk.
The way the protocol is designed doesn't requires any prior communication.
The chunk size may be decided as late as when the chunk is ready to be sent.

Note: ClipDataEx can easily be modified to do this
because it doesn't prepares protocol messages until the moment they're needed.
Unsent data is simply kept in a byte array.

Note: Due to a limit of the underlying image type,
the width of a ClipDataEx image will always be a multiple of 4 pixels.

## Data Encoding

Quick overview of how data is encoded

1. Take an unsent segment of a file not exceeding `0xFFFFFF` bytes
2. Store this in a clipboard exchange structure (see further below)
3. If this is the first part, supply the file name and file size header field
4. Serialize the structure
5. Encrypt the structure (see further below)
6. Prefix the encrypted data using 16 randomly chosen bytes
7. Encode the prefixed data into an image
8. Save the image into the clipboard

## Control Messages

Messages can be sent that do not contain file data,
among other things, this includes chat messages
as well as acknowledging file parts that were received.

Control messages are encoded in an identical fashion to data messages,
except that they lack data (step 1 from "data encoding")
and instead are a mostly empty clipboard exchange structure.

## Data structure

This chapter explains how the raw data is structured.

- Sizes are given in bytes unless otherwise specified
- An asterisk means the field has no hardcoded size
- Multi-byte numbers are stored in big endian format
- Data types are given as C# types

### Clipboard Exchange Structure

| Field  | Size      | Description                                   |
|--------|-----------|-----------------------------------------------|
| Type   | 1         | Message type                                  |
| Header | *         | Header field (repeatable as often as desired) |
| Zero   | 1         | A nullbyte to mark the end of the header      |
| Length | 3         | Length of the data following. Can be zero     |
| Data   | *         | As many bytes as the Length field specifies   |

The structure is identical for all message types.
This means even types that normally don't contain data can contain data,
which will be ignored by the client however.
This data can be used to pad the messages
and obfuscate whether an image is data or a control message.
Doing that slows down the transfer process however.

Being just a proof of concept, ClipDataEx doesn't creates padding,
but can handle padded messages it receives.

### Message Type

The following message types are defined:

- `0x00` Message contains file data
- `0x01` Acknowledge receiving of a file part
- `0x02` Request a response from the other client
- `0x03` Response to a 0x02 message
- `0x04` Text message

Except for 0x00 and 0x04, the data portion of the message will be ignored.

#### Data format

- For type 0x00, the data is the raw data of the file segment.
- For type 0x04, the data is an UTF-8 encoded string of at most 1000 bytes.

### Clipboard Exchange Structure Header

A header has this format:

| Field | Size | Description                           |
|-------|------|---------------------------------------|
| Type  | 1    | Field type                            |
| Size  | 2    | Length of following data. Can be zero |
| Data  | *    | As many bytes as "Length" specifies   |

A clipboard exange structure has no limit on the number of headers,
but reasonably speaking, a client should abort after 10+ headers.
Messages that would need this many headers should instead use the data portion.

Identical header types can appear multiple times,
there is currently no header type that would mandate this feature however.

As of now, duplicate header types in ClipDataEx simply overwrite previous occurences.

Headers can appear in any order.

### Clipboard Exchange Structure Header Types

The following header types are defined:

| Number | Name            | Type   | Description                              |
|--------|-----------------|--------|------------------------------------------|
| `0x00` | Reserved        | N/A    | Reserved as the "End of Header" mark     |
| `0x01` | File Id         | Guid   | Id unique to every file                  |
| `0x02` | File Name       | string | UTF-8 encoded file name (no path)        |
| `0x03` | File Size       | ulong  | Size of the file (not just this segment) |
| `0x04` | Sequence Number | uint   | Sequence number of the current segment   |
| `0x05` | Sender Id       | uint   | Id of the sender                         |

#### 0x00 Reserved

This is not really a header.
If this type is read, then hader parsing must immediately be stopped.
There will be no size and no data.
This simply is the way to tell when the headers end.
This is the "Zero" field mentioned in the clipboard exchange structure.

#### File Id

This is an id that is unique to every file but identical across file segments.
This is used in the receiving end to associate segments with the correct file.

**This field is mandatory in every 0x00 and 0x01 message**

#### File Name

This is the file name of the file. It should not contain path information.
The name is to be encoded as UTF-8.

**This field is mandatory in the first 0x00 segment and ignored in subsequent segments**

#### File Size

Size of the file in bytes.
This is used by the recipient to detect when a file has been completely received.

**This field is mandatory in the first 0x00 segment and ignored in subsequent segments**

#### Sequence Number

This is the sequence number of file parts.
It starts at zero with the first part
and increases for every file part that is sent.
It's used by the recipient to put the data into the correct order
and to ignore duplicate segments.

**This field is mandatory in every 0x00 and 0x01 message**

#### Sender Id

This is the id of the sender.
It's simply a 4 byte value that is never changed.

Note: This is a 4 byte value on purpose,
this allows a client to treat this like an IPv4 address,
which in turn would allow you to run a fake IP network over the clipboard.
IPoCB (IP over Clipboard) has not been invented yet as far as I know.

**This field is mandatory in every protocol message**

### Encrypted Data

Data is encrypted using AES-GCM.

The encrypted data is simply the concatenation of nonce, tag, ciphertext.

The values are not prefixed, but the sizes are known
because ClipDataEx picks the largest valid nonce and tag sizes.

Note: The order of these 3 fields must not be changed or decryption fails.

ClipDataEx currently implements password based key by feeding the key through SHA256.
As a proof of concept this is not a problem because the hash is never transmitted,
but if you intend on using this file transfer method,
consider using a key derivation function like PBKDF2 for this instead.

This of course makes a new data packet necessary to exchange the nonce.

### Raw Data

The raw data is the encrypted data prefixed with 16 randomly chosen bytes.

There's no meaning in those bytes,
but the recipient can use them to avoid processing the data twice.
This is a useful time saver
because images are sometimes encoded multiple times in the clipboard
to accomodate different formats.
A duplicate id thus indicates a duplicate packet.

## Example data structure

*Line breaks are used to split long lines in this document.*
*They're not present in the source data*

Example file data for the first segment of a file transfer:

    <16-bytes><nonce><tag><encrypted>

Segment after being decrypted:

    <clipboard-exchange-structure>

Contents of the structure:

    <type:0x00><header1:FileName><header2:FileSize><header3:SenderId>
	<header4:SequenceNumber><header5:FileId><zero><data-length><data>

Contents of header1 (file name):

	<type:0x02><length:8><data:Test.txt>

Contents of header2 (file size):

	<type:0x03><length:8><data:123456>

Contents of header3 (client id):

	<type:0x05><length:4><data:1222773457>

Contents of header4 (sequence number):

	<type:0x04><length:4><data:0>

Contents of header5 (file id):

	<type:0x01><length:16><data:5526F5A9-9FD3-41C8-9691-EE13B0D927B5>

## Acknowledging Transfers

If a client successfully decoded a packet,
he immediately acknowledges the transfer.

This is done by sending a Clipboard Exchange Structure back.
The structure must have these headers set:

- Sender Id
- File Id
- Sequence Number

If a client receives an acknowledge packet
he checks if the packet is for the last file segment he sent.
If the fields match up, he sends the next file segment,
provided there are any unsent file bytes left.

## Limitations

Being a proof of concept, ClipDataEx doesn't implements everything
that this standard would allow. Notably, these features are missing:

- Handling 3 or more clients
- Queueing multiple files
- Progress indicator at the sender

## Using More Than Two Devices

Using more than two devices is possible, but ClipDataEx doesn't implements this.
To have more than two clients working reliably, you would need to add a few things:

### Client Awareness

Each client needs to be aware of other clients.
This is not a big problem, the ping/pong packets can be used for this.
A client that comes online and sends a ping would get a pong from all other clients,
and the other clients would see a new client id in the ping packet.

### Id Conflict Resolving

Although unlikely,
it's possible for a client to pick an id that's already occupied.

### Multiple Acknowledgements

The sending client needs to track which clients acknowledged a file part.
ClipDataEx just sends the next part on any matching acknowledge,
but for more than two clients it should wait until **all** clients responded,
or resend the segment if a client doesn't answers for too long.

### Dropping Clients

There must be a mechanism to drop inactive clients,
and a new packet type for a client leaving the group.
The leave packet should be sent whenever the client id or password changes,
or when the client exits, provided the "network" is idle.
If there is an ongoing transfer it's better to let the next acknowledge time out.

### Keep Alive

If the clipboard is inactive, clients should occasionally ping.
To avoid collisions, the client with the lowest id should send a ping,
if none is received after a defined timeout, clients drop the lowest id,
and the ping send progress starts with the next client.
Other clients do not need to ping because they send pong,
and thus already prove that they're online.

### Collision Detection and Avoidance

The clipboard is like multiple devices sharing the same wire.
If two clients send files at the same time,
one of the packets will likely get lost.
This means that all clients must monitor the clipboard at all time
to avoid problems such as two clients trying to send a file simultaneously.

There should also be a defined order in which packets are sent.
Since all clients know all other clients,
the easiest way is to send by client id ascending.

### Destination Addressing

This is currently basically a broadcast network,
and everyone receives every packet.
A new header with the destination id could help in this case.
This would be the first header that can legitimately be repeated,
to address multiple recipients at once.
