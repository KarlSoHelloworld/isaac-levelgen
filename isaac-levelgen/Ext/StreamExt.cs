﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isaac_levelgen
{
	public static class StreamExt
	{
		[ThreadStatic]
		static byte[] _buffer;
		static byte[] buffer {
			get { return _buffer ?? (_buffer = new byte[16]); }
		}

		public static void FillBuffer(this Stream stream, int numBytes) {
			if ((numBytes < 0x0) || (numBytes > buffer.Length)) {
				throw new ArgumentOutOfRangeException("numBytes");
			}
			if (numBytes < 1)
				return;

			int read;
			if (numBytes == 0x1) {
				read = stream.ReadByte();
				if (read == -1) {
					throw new EndOfStreamException("End of stream");
				}
				buffer[0x0] = (byte)read;
			} else {
				int offset = 0x0;
				do {
					read = stream.Read(buffer, offset, numBytes - offset);
					if (read == 0x0) {
						throw new EndOfStreamException("End of stream");
					}
					offset += read;
				}
				while (offset < numBytes);
			}

		}
		public static void WriteBoolean(this Stream s, bool value) {
			s.WriteInt8(value ? ((byte)0x1) : ((byte)0x0));
		}
		public static void WriteInt8(this Stream s, byte num) {
			s.WriteByte(num);
		}
		public static void WriteInt16(this Stream s, Int16 value) {
			buffer[0x0] = (byte)value;
			buffer[0x1] = (byte)(value >> 0x8);
			s.Write(buffer, 0x0, 0x2);
		}
		public static void WriteInt32(this Stream s, Int32 value) {
			buffer[0x0] = (byte)value;
			buffer[0x1] = (byte)(value >> 0x8);
			buffer[0x2] = (byte)(value >> 0x10);
			buffer[0x3] = (byte)(value >> 0x18);
			s.Write(buffer, 0x0, 0x4);
		}
		public static void WriteInt64(this Stream s, Int64 value) {
			buffer[0x0] = (byte)value;
			buffer[0x1] = (byte)(value >> 0x8);
			buffer[0x2] = (byte)(value >> 0x10);
			buffer[0x3] = (byte)(value >> 0x18);
			buffer[0x4] = (byte)(value >> 0x20);
			buffer[0x5] = (byte)(value >> 0x28);
			buffer[0x6] = (byte)(value >> 0x30);
			buffer[0x7] = (byte)(value >> 0x38);
			s.Write(buffer, 0x0, 0x8);
		}
		public static unsafe void WriteDouble(this Stream s, double num) {
			Int64 n1 = *((Int64*)&num);
			s.WriteInt64(n1);
		}
		public static unsafe void WriteSingle(this Stream s, float num) {
			var n1 = *((Int32*)&num);
			s.WriteInt32(n1);
		}
		public static void WriteBytesWithLength(this Stream s, byte[] bytes) {
			s.WriteInt32(bytes.Length);
			s.WriteBytes(bytes);
		}
		public static void WriteBytes(this Stream s, byte[] bytes, Int32 len) {
			s.Write(bytes, 0, len);
		}
		public static void WriteBytes(this Stream s, byte[] bytes) {
			s.Write(bytes, 0, bytes.Length);
		}
		public static void WriteString(this Stream s, string str) {
			if (str == null)
				str = string.Empty;

			s.WriteEncodedInt((Int32)str.Length);
			if (str.Length > 0)
				s.WriteBytes(Encoding.UTF8.GetBytes(str));
		}
		public static void WriteEncodedInt(this Stream s, int value) {
			uint num = (uint)value;
			while (num >= 0x80) {
				s.WriteInt8((byte)(num | 0x80));
				num = num >> 0x7;
			}
			s.WriteInt8((byte)num);
		}

		public static byte ReadInt8(this Stream s) {
			int read = s.ReadByte();
			if (read == -1) {
				throw new EndOfStreamException("End of stream");
			}
			return (byte)read;
		}
		public static bool ReadBoolean(this Stream s) {
			return s.ReadInt8() != 0;
		}

		public static Int16 ReadInt16(this Stream s) {
			s.FillBuffer(0x2);
			return (Int16)(buffer[0x0] | (buffer[0x1] << 0x8));
		}
		public static UInt16 ReadUInt16(this Stream s) {
			return (UInt16)s.ReadInt16();
		}

		public static Int32 ReadInt32(this Stream s) {
			s.FillBuffer(0x4);
			return (((buffer[0x0] | (buffer[0x1] << 0x8)) | (buffer[0x2] << 0x10)) | (buffer[0x3] << 0x18));

		}
		public static UInt32 ReadUInt32(this Stream s) {
			return (UInt32)s.ReadInt32();
		}

		public static Int64 ReadInt64(this Stream s) {
			s.FillBuffer(0x8);
			UInt64 num = (UInt32)(((buffer[0x0] | (buffer[0x1] << 0x8)) | (buffer[0x2] << 0x10)) | (buffer[0x3] << 0x18));
			UInt64 num2 = (UInt32)(((buffer[0x4] | (buffer[0x5] << 0x8)) | (buffer[0x6] << 0x10)) | (buffer[0x7] << 0x18));
			return (Int64)((num2 << 0x20) | num);

		}
		public static UInt64 ReadUInt64(this Stream s) {
			return (UInt64)s.ReadInt64();
		}

		public static unsafe double ReadDouble(this Stream s) {
			var ret = (UInt64)s.ReadUInt64();
			return *((double*)&ret);
		}

		public static unsafe float ReadSingle(this Stream s) {
			var ret = s.ReadUInt32();
			return *((float*)&ret);
		}

		public static byte[] ReadBytesWithLength(this Stream s) {
			Int32 len = s.ReadInt32();
			return s.ReadBytes(len);
		}
		public static byte[] ReadBytes(this Stream s, Int32 count) {
			if (count < 0x0) {
				throw new ArgumentOutOfRangeException("count");
			}

			byte[] buffer = new byte[count];
			int offset = 0x0;
			do {
				int num2 = s.Read(buffer, offset, count);
				if (num2 == 0x0) {
					break;
				}
				offset += num2;
				count -= num2;
			}
			while (count > 0x0);
			if (offset != buffer.Length) {
				byte[] dst = new byte[offset];
				Buffer.BlockCopy(buffer, 0x0, dst, 0x0, offset);
				buffer = dst;
			}
			return buffer;

		}
		public static string ReadString(this Stream s) {
			int len = s.ReadEncodedInt();
			if (len > 0)
				return Encoding.UTF8.GetString(s.ReadBytes(len));
			return string.Empty;
		}

		public static string ReadLine(this Stream s) {
			var ret = new StringBuilder();
			char c;
			while ((c = (char)s.ReadInt8()) != '\n') {
				if (c != '\r')
					ret.Append(c);
			}
			return ret.ToString();
		}

		public static string ReadStringNullTerm(this Stream s) {
			var ret = new StringBuilder();
			byte c;
			while ((c = s.ReadInt8()) != 0)
				ret.Append((char)c);
			return ret.ToString();
		}

		public static int ReadEncodedInt(this Stream s) {
			byte num3;
			int num = 0x0;
			int num2 = 0x0;
			do {
				if (num2 == 0x23) {
					throw new FormatException("Format_Bad7BitInt32");
				}
				num3 = s.ReadInt8();
				num |= (num3 & 0x7f) << num2;
				num2 += 0x7;
			}
			while ((num3 & 0x80) != 0x0);
			return num;
		}


		public static void InternalCopyTo(this Stream source, Stream destination, int bufferSize) {
			int num;
			var buffer = new byte[bufferSize];
			while ((num = source.Read(buffer, 0, buffer.Length)) != 0) {
				destination.Write(buffer, 0, num);
			}
		}
		public static void CopyTo(this Stream source, Stream destination) {
			source.InternalCopyTo(destination, 0x1000);
		}

		public static byte[] CopyToArray(this Stream source) {
			if (source is MemoryStream)
				return ((MemoryStream)source).ToArray();
			using (var ms = new MemoryStream()) {
				source.CopyTo(ms);
				return ms.ToArray();
			}
		}
	}
}
