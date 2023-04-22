# ClipDataEx

ClipDataEx is a tool to exfiltrate data via clipboard.

It serves as a proof of concept, but is fully functional.
It may lack a few non-essential features
like a progress indicator on the sender side.

## Short Description

ClipDataEx can send files between any two machines on the world
that share the same clipboard and allow images to be exchanged.

This adds file sharing capabilities to virtually any screen sharing technology,
including but not limited to VNC, RDP, Citrix,
but also virtual machines such as VMWare and VirtualBox,
provided clipboard sharing is enabled.

Data is protected using encryption and integrity functions
to prevent people from peeking at the data or modifying it.

## Detailed Description

ClipDataEx sends files by encoding them inside of the pixel data of images.
The images are created in memory as needed.

Note: ClipDataEx does not perform steganography.
The images exclusively contain the transmitted data.
Because of the encryption, this makes the pixels appear completely random,
and made up of white noise.

### How Files are Transferred

The file is split up into chunks of data.
ClipDataEx has a hardcoded size of approximately 1 MB
to accomodate progress reporting on slow connections.
The theoretical limit per chunk is `0xFFFFFF` (16777215) bytes.
This would result in an image of approximately 2365x2365 pixels.
With 1 MB, the images are 592x592 pixels, which is a more reasonable size.

Note: Within the permitted size limits, chunks can be of any size.
ClipDataEx doesn't has this feature built-in,
but a user might decide to randomize the chunk size for every sent chunk.

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

### Control Messages

Control messages are encoded in an identical fashion,
except that they lack data (step 1)
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
Being just a proof of concept, ClipDataEx doesn't implements this.

### Message Type

The following message types are defined:

- `0x00` Message contains file data
- `0x01` Acknowledge receiving of a file part
- `0x02` Request a response from the other client
- `0x03` Response to a 0x02 message
- `0x04` Text message

Except for 0x01 and 0x04, the data portion of the message will be ignored.

#### Data format

- For type 0x01, the data is the raw data of the file segment.
- For type 0x04, the data is an UTF-8 encoded string of at most 1000 bytes.

### Clipboard Exchange Structure Header

A header has this format:

| Field | Size | Description                           |
|-------|------|---------------------------------------|
| Type  | 1    | Field type                            |
| Size  | 2    | Length of following data. Can be zero |
| Data  | *    | As many bytes as "Length" specifies   |

A clipboard exange structure has no limit on the number of headers.
Identical header types can appear multiple times too,
however, there is currently no header type that would mandate this feature.
Duplicate header types in ClipDataEx simply overwrite previous occurences.

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

**This field is mandatory in every 0x01 file segment message**

#### File Name

This is the file name of the file. It should not contain path information.
The name is to be encoded as UTF-8.

**This field is mandatory in the first 0x01 segment and ignored in subsequent segments**

#### File Size

Size of the file in bytes.
This is used by the recipient to detect when a file has been completely received.

**This field is mandatory in the first 0x01 segment and ignored in subsequent segments**

#### Sequence Number

This is the sequence number of file parts.
It starts at zero with the first part
and increases for every file part that is sent.
It's used by the recipient to put the data into the correct order
and to ignore duplicate segments.

**This field is mandatory in the first 0x01 segment and ignored in subsequent segments**

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

Note: The order must not be changed or decryption fails.

### Raw Data

The raw data is the encrypted data prefixed with 16 randomly chosen bytes.

There's no meaning in those bytes,
but the recipient can use them to avoid processing the data twice.
This is a useful time saver
because images are sometimes encoded multiple times in the clipboard.
A duplicate id thus indicates a duplicate packet.

### Acknowledging Transfers

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
