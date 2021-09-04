using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Momo.Core
{
	public struct Flags32<TEnum> : IEquatable<Flags32<TEnum>>, IConvertible
		where TEnum : struct, Enum
	{
		public int Flags;

		public Flags32(TEnum value)
		{
			CheckEnumSize();

			Flags = UnsafeUtility.EnumToInt(value);
		}

		public bool TestAll(Flags32<TEnum> requiredFlags)
		{
			return ((Flags & requiredFlags.Flags) == requiredFlags.Flags);
		}

		public bool TestAny(Flags32<TEnum> flags)
		{
			return ((Flags & flags.Flags) != 0);
		}

		public void Set(Flags32<TEnum> flagsToSet)
		{
			Flags |= flagsToSet.Flags;
		}

		public void Clear(Flags32<TEnum> flagsToClear)
		{
			Flags &= ~(flagsToClear.Flags);
		}

		public void Clear()
		{
			Flags = 0;
		}

		public void Assign(Flags32<TEnum> flagsToAssign, bool value)
		{
			if (value)
			{
				Set(flagsToAssign);
			}
			else
			{
				Clear(flagsToAssign);
			}
		}

		public int CountBits()
		{
			// Hamming weight
			int value = Flags;
			value = value - ((value >> 1) & 0x55555555);
			value = (value & 0x33333333) + ((value >> 2) & 0x33333333);
			int count = ((value + (value >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;

			return count;
		}

		public static implicit operator Flags32<TEnum>(TEnum e)
		{
			return new Flags32<TEnum>(e);
		}

		[BurstDiscard]
		[Conditional("DEBUG")]
		private static void CheckEnumSize()
		{
			if (UnsafeUtility.SizeOf<TEnum>() != UnsafeUtility.SizeOf<int>())
			{
				throw new Exception($"Backing datatype of enum '{typeof(TEnum).Name}' should be 32 bits!");
			}
		}

		public override string ToString()
		{
			return ((TEnum)(object)Flags).ToString();
		}

		#region IEquatable

		public static bool operator ==(Flags32<TEnum> lhs, Flags32<TEnum> rhs)
		{
			return lhs.Flags == rhs.Flags;
		}

		public static bool operator !=(Flags32<TEnum> lhs, Flags32<TEnum> rhs)
		{
			return lhs.Flags != rhs.Flags;
		}

		public bool Equals(Flags32<TEnum> other)
		{
			return this == other;
		}

		public override bool Equals(object obj)
		{
			return obj is Flags32<TEnum> other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Flags;
		}

		#endregion

		#region IConvertible

		public int ToInt32(IFormatProvider provider)
		{
			return Flags;
		}

		public TypeCode GetTypeCode()
		{
			throw new NotImplementedException();
		}

		public bool ToBoolean(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public byte ToByte(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public char ToChar(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public DateTime ToDateTime(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public decimal ToDecimal(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public double ToDouble(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public short ToInt16(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public long ToInt64(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public sbyte ToSByte(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public float ToSingle(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public string ToString(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public object ToType(Type conversionType, IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public ushort ToUInt16(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public uint ToUInt32(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public ulong ToUInt64(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
