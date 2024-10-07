namespace SeaOfNodes.Lib;

using System;
using System.Numerics;
using System.Text;

/* BitSet.java -- A vector of bits.
Copyright (C) 1998, 1999, 2000, 2001, 2004, 2005  Free Software Foundation, Inc.

This file is part of GNU Classpath.

GNU Classpath is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2, or (at your option)
any later version.

GNU Classpath is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
General Public License for more details.

You should have received a copy of the GNU General Public License
along with GNU Classpath; see the file COPYING.  If not, write to the
Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA
02110-1301 USA.

Linking this library statically or dynamically with other modules is
making a combined work based on this library.  Thus, the terms and
conditions of the GNU General Public License cover the whole
combination.

As a special exception, the copyright holders of this library give you
permission to link this library with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module.  An independent module is a module which is not derived from
or based on this library.  If you modify this library, you may extend
this exception to your version of the library, but you are not
obligated to do so.  If you do not wish to do so, delete this
exception statement from your version. */


/* Written using "Java Class Libraries", 2nd edition, ISBN 0-201-31002-3
* hashCode algorithm taken from JDK 1.2 docs.
*/

/**
* This class can be thought of in two ways.  You can see it as a
* vector of bits or as a set of non-negative integers.  The name
* <code>BitSet</code> is a bit misleading.
*
* It is implemented by a bit vector, but its equally possible to see
* it as set of non-negative integer; each integer in the set is
* represented by a set bit at the corresponding index.  The size of
* this structure is determined by the highest integer in the set.
*
* You can union, intersect and build (symmetric) remainders, by
* invoking the logical operations and, or, andNot, resp. xor.
*
* This implementation is NOT synchronized against concurrent access from
* multiple threads. Specifically, if one thread is reading from a bitset
* while another thread is simultaneously modifying it, the results are
* undefined.
*
* @author Jochen Hoenicke
* @author Tom Tromey (tromey@cygnus.com)
* @author Eric Blake (ebb9@email.byu.edu)
* @status updated to 1.4
*/
public class BitSet
{
    /**
    * Compatible with JDK 1.0.
*/
    private const long serialVersionUID = 7997698588986878753L;

    /**
    * A common mask.
*/
    private const int LONG_MASK = 0x3f;

    /**
    * The actual bits.
    * @serial the i'th bit is in bits[i/64] at position i%64 (where position
    *         0 is the least significant).
*/
    private ulong[] bits;

    /**
    * Create a new empty bit set. All bits are initially false.
*/
    public BitSet() :

    this(64)
    {
    }

    /**
    * Create a new empty bit set, with a given size.  This
    * constructor reserves enough space to represent the integers
    * from <code>0</code> to <code>nbits-1</code>.
    *
    * @param nbits the initial size of the bit set
    * @throws NegativeArraySizeException if nbits &lt; 0
*/
    public BitSet(int nbits)
    {
        if (nbits < 0)
            throw new ArgumentOutOfRangeException();

        int length = nbits >>> 6;
        if ((nbits & LONG_MASK) != 0)
            ++length;
        bits = new ulong[length];
    }

    public BitSet(BitSet that)
    {
        this.bits = new ulong[that.bits.Length];
        Array.Copy(that.bits, this.bits, this.bits.Length);
    }

    /**
    * Performs the logical AND operation on this bit set and the
    * given <code>set</code>.  This means it builds the intersection
    * of the two sets.  The result is stored into this bit set.
    *
    * @param bs the second bit set
    * @throws NullPointerException if bs is null
*/
    public void and(BitSet bs)
    {
        int max = Math.Min(bits.Length, bs.bits.Length);
        int i;
        for (i = 0; i < max; ++i)
            bits[i] &= bs.bits[i];
        while (i < bits.Length)
            bits[i++] = 0;
    }

    /**
    * Performs the logical AND operation on this bit set and the
    * complement of the given <code>bs</code>.  This means it
    * selects every element in the first set, that isn't in the
    * second set.  The result is stored into this bit set and is
    * effectively the set difference of the two.
    *
    * @param bs the second bit set
    * @throws NullPointerException if bs is null
    * @since 1.2
*/
    public void andNot(BitSet bs)
    {
        int i = Math.Min(bits.Length, bs.bits.Length);
        while (--i >= 0)
            bits[i] &= ~bs.bits[i];
    }

    /**
    * Returns the number of bits set to true.
    *
    * @return the number of true bits
    * @since 1.4
*/
    public int cardinality()
    {
        int card = 0;
        for (int i = bits.Length - 1; i >= 0; i--)
        {
            ulong a = bits[i];
            card = BitOperations.PopCount(a);
        }
        return card;
    }

    /// <summary>
    ///Sets all bits in the set to false.
    ///
    ///@since 1.4
    /// </summary>
    public void clear()
    {
        Array.Clear(bits);
    }

    /**
    * Removes the integer <code>pos</code> from this set. That is
    * the corresponding bit is cleared.  If the index is not in the set,
    * this method does nothing.
    *
    * @param pos a non-negative integer
    * @throws IndexOutOfBoundsException if pos &lt; 0
*/
    public void clear(int pos)
    {
        int offset = pos >> 6;
        ensure(offset);
        // ArrayIndexOutOfBoundsException subclasses IndexOutOfBoundsException,
        // so we'll just let that be our exception.
        bits[offset] &= ~(1uL << pos);
    }

    /**
    * Sets the bits between from (inclusive) and to (exclusive) to false.
    *
    * @param from the start range (inclusive)
    * @param to the end range (exclusive)
    * @throws IndexOutOfBoundsException if from &lt; 0 || to &lt; 0 ||
    *         from &gt; to
    * @since 1.4
*/
    public void clear(int from, int to)
    {
        if (from < 0 || from > to)
            throw new ArgumentOutOfRangeException();
        if (from == to)
            return;
        int lo_offset = from >>> 6;
        int hi_offset = to >>> 6;
        ensure(hi_offset);
        if (lo_offset == hi_offset)
        {
            bits[hi_offset] &= ((1uL << from) - 1) | (~0uL << to);
            return;
        }

        bits[lo_offset] &= (1uL << from) - 1;
        bits[hi_offset] &= ~0uL << to;
        for (int i = lo_offset + 1; i < hi_offset; i++)
            bits[i] = 0;
    }

    /**
    * Create a clone of this bit set, that is an instance of the same
    * class and contains the same elements.  But it doesn't change when
    * this bit set changes.
    *
    * @return the clone of this object.
*/
    public object clone()
    {
        BitSet bs = new BitSet(this);
        return bs;
    }

    /**
    * Returns true if the <code>obj</code> is a bit set that contains
    * exactly the same elements as this bit set, otherwise false.
    *
    * @param obj the object to compare to
    * @return true if obj equals this bit set
*/
    public override bool Equals(object? obj)
    {
        if (obj is not BitSet bs)
            return false;
        int max = Math.Min(bits.Length, bs.bits.Length);
        int i;
        for (i = 0; i < max; ++i)
            if (bits[i] != bs.bits[i])
                return false;
        // If one is larger, check to make sure all extra bits are 0.
        for (int j = i; j < bits.Length; ++j)
            if (bits[j] != 0)
                return false;
        for (int j = i; j < bs.bits.Length; ++j)
            if (bs.bits[j] != 0)
                return false;
        return true;
    }

    /**
    * Sets the bit at the index to the opposite value.
    *
    * @param index the index of the bit
    * @throws IndexOutOfBoundsException if index is negative
    * @since 1.4
*/
    public void flip(int index)
    {
        int offset = index >> 6;
        ensure(offset);
        // ArrayIndexOutOfBoundsException subclasses IndexOutOfBoundsException,
        // so we'll just let that be our exception.
        bits[offset] ^= 1uL << index;
    }

    /**
    * Sets a range of bits to the opposite value.
    *
    * @param from the low index (inclusive)
    * @param to the high index (exclusive)
    * @throws IndexOutOfBoundsException if from &gt; to || from &lt; 0 ||
    *         to &lt; 0
    * @since 1.4
*/
    public void flip(int from, int to)
    {
        if (from < 0 || from > to)
            throw new ArgumentOutOfRangeException();
        if (from == to)
            return;
        int lo_offset = from >>> 6;
        int hi_offset = to >>> 6;
        ensure(hi_offset);
        if (lo_offset == hi_offset)
        {
            bits[hi_offset] ^= (~0ul << from) & ((1uL << to) - 1);
            return;
        }

        bits[lo_offset] ^= ~0ul << from;
        bits[hi_offset] ^= (1uL << to) - 1;
        for (int i = lo_offset + 1; i < hi_offset; i++)
            bits[i] = ~bits[i];
    }

    /**
    * Returns true if the integer <code>bitIndex</code> is in this bit
    * set, otherwise false.
    *
    * @param pos a non-negative integer
    * @return the value of the bit at the specified position
    * @throws IndexOutOfBoundsException if the pos is negative
*/
    public bool get(int pos)
    {
        int offset = pos >> 6;
        if (offset >= bits.Length)
            return false;
        // ArrayIndexOutOfBoundsException subclasses IndexOutOfBoundsException,
        // so we'll just let that be our exception.
        return (bits[offset] & (1uL << pos)) != 0;
    }

    /**
    * Returns a new <code>BitSet</code> composed of a range of bits from
    * this one.
    *
    * @param from the low index (inclusive)
    * @param to the high index (exclusive)
    * @throws IndexOutOfBoundsException if from &gt; to || from &lt; 0 ||
    *         to &lt; 0
    * @since 1.4
*/
    public BitSet get(int from, int to)
    {
        if (from < 0 || from > to)
            throw new ArgumentOutOfRangeException();
        BitSet bs = new BitSet(to - from);
        int lo_offset = from >>> 6;
        if (lo_offset >= bits.Length || to == from)
            return bs;

        int lo_bit = from & LONG_MASK;
        int hi_offset = to >>> 6;
        if (lo_bit == 0)
        {
            int len = Math.Min(hi_offset - lo_offset + 1, bits.Length - lo_offset);
            Array.Copy(bits, lo_offset, bs.bits, 0, len);
            if (hi_offset < bits.Length)
                bs.bits[hi_offset - lo_offset] &= (1uL << to) - 1;
            return bs;
        }

        int xlen = Math.Min(hi_offset, bits.Length - 1);
        int reverse = 64 - lo_bit;
        int i;
        for (i = 0; lo_offset < xlen; lo_offset++, i++)
            bs.bits[i] = ((bits[lo_offset] >>> lo_bit)
                          | (bits[lo_offset + 1] << reverse));
        if ((to & LONG_MASK) > lo_bit)
            bs.bits[i++] = bits[lo_offset] >>> lo_bit;
        if (hi_offset < bits.Length)
            bs.bits[i - 1] &= (1uL << (to - from)) - 1;
        return bs;
    }

    /**
    * Returns a hash code value for this bit set.  The hash code of
    * two bit sets containing the same integers is identical.  The algorithm
    * used to compute it is as follows:
    *
    * Suppose the bits in the BitSet were to be stored in an array of
    * long integers called <code>bits</code>, in such a manner that
    * bit <code>k</code> is set in the BitSet (for non-negative values
    * of <code>k</code>) if and only if
    *
    * <code>((k/64) &lt; bits.length)
    * && ((bits[k/64] & (1uL &lt;&lt; (bit % 64))) != 0)
    * </code>
    *
    * Then the following definition of the hashCode method
    * would be a correct implementation of the actual algorithm:
    *
    * 
<pre>public int hashCode()
{
long h = 1234;
for (int i = bits.length-1; i &gt;= 0; i--)
{
h ^= bits[i] * (i + 1);
}

return (int)((h >> 32) ^ h);
}</pre>
    *
    * Note that the hash code values changes, if the set is changed.
    *
    * @return the hash code value for this bit set.
*/
    public override int GetHashCode()
    {
        int h = 1234;
        for (int i = bits.Length; i > 0;)
            h ^= HashCode.Combine(i, bits[--i]);
        return h;
    }

    /**
    * Returns true if the specified BitSet and this one share at least one
    * common true bit.
    *
    * @param set the set to check for intersection
    * @return true if the sets intersect
    * @throws NullPointerException if set is null
    * @since 1.4
*/
    public bool intersects(BitSet set)
    {
        int i = Math.Min(bits.Length, set.bits.Length);
        while (--i >= 0)
            if ((bits[i] & set.bits[i]) != 0)
                return true;
        return false;
    }

    /**
    * Returns true if this set contains no true bits.
    *
    * @return true if all bits are false
    * @since 1.4
*/
    public bool isEmpty()
    {
        for (int i = bits.Length - 1; i >= 0; i--)
            if (bits[i] != 0)
                return false;
        return true;
    }

    /**
    * Returns the logical number of bits actually used by this bit
    * set.  It returns the index of the highest set bit plus one.
    * Note that this method doesn't return the number of set bits.
    *
    * @return the index of the highest set bit plus one.
*/
    public int length()
    {
        // Set i to highest index that contains a non-zero value.
        int i;
        for (i = bits.Length - 1; i >= 0 && bits[i] == 0; --i)
            ;

        // if i < 0 all bits are cleared.
        if (i < 0)
            return 0;

        // Now determine the exact length.
        long b = (long)bits[i];
        int len = (i + 1) * 64;
        // b >= 0 checks if the highest bit is zero.
        while (b >= 0)
        {
            --len;
            b <<= 1;
        }

        return len;
    }

    /**
    * Returns the index of the next false bit, from the specified bit
    * (inclusive).
    *
    * @param from the start location
    * @return the first false bit
    * @throws IndexOutOfBoundsException if from is negative
    * @since 1.4
*/
    public int nextClearBit(int from)
    {
        int offset = from >> 6;
        ulong mask = 1uL << from;
        while (offset < bits.Length)
        {
            // ArrayIndexOutOfBoundsException subclasses IndexOutOfBoundsException,
            // so we'll just let that be our exception.
            ulong h = bits[offset];
            do
            {
                if ((h & mask) == 0)
                    return from;
                mask <<= 1;
                from++;
            }
            while (mask != 0);
            mask = 1;
            offset++;
        }
        return from;
    }

    /**
    * Returns the index of the next true bit, from the specified bit
    * (inclusive). If there is none, -1 is returned. You can iterate over
    * all true bits with this loop:<br>
    * 
<pre>for (int i = bs.nextSetBit(0); i &gt;= 0; i = bs.nextSetBit(i + 1))
{
// operate on i here
}</pre>
    *
    * @param from the start location
    * @return the first true bit, or -1
    * @throws IndexOutOfBoundsException if from is negative
    * @since 1.4
*/
    public int nextSetBit(int from)
    {
        int offset = from >> 6;
        ulong mask = 1uL << from;
        while (offset < bits.Length)
        {
            // ArrayIndexOutOfBoundsException subclasses IndexOutOfBoundsException,
            // so we'll just let that be our exception.
            ulong h = bits[offset];
            do
            {
                if ((h & mask) != 0)
                    return from;
                mask <<= 1;
                from++;
            }
            while (mask != 0);
            mask = 1;
            offset++;
        }
        return -1;
    }

    /**
    * Performs the logical OR operation on this bit set and the
    * given <code>set</code>.  This means it builds the union
    * of the two sets.  The result is stored into this bit set, which
    * grows as necessary.
    *
    * @param bs the second bit set
    * @throws NullPointerException if bs is null
*/
    public void or(BitSet bs)
    {
        ensure(bs.bits.Length - 1);
        for (int i = bs.bits.Length - 1; i >= 0; i--)
            bits[i] |= bs.bits[i];
    }

    /**
    * Add the integer <code>bitIndex</code> to this set.  That is
    * the corresponding bit is set to true.  If the index was already in
    * the set, this method does nothing.  The size of this structure
    * is automatically increased as necessary.
    *
    * @param pos a non-negative integer.
    * @throws IndexOutOfBoundsException if pos is negative
*/
    public void set(int pos)
    {
        int offset = pos >> 6;
        ensure(offset);
        // ArrayIndexOutOfBoundsException subclasses IndexOutOfBoundsException,
        // so we'll just let that be our exception.
        bits[offset] |= 1uL << pos;
    }

    /**
    * Sets the bit at the given index to the specified value. The size of
    * this structure is automatically increased as necessary.
    *
    * @param index the position to set
    * @param value the value to set it to
    * @throws IndexOutOfBoundsException if index is negative
    * @since 1.4
*/
    public void set(int index, bool value)
    {
        if (value)
            set(index);
        else
            clear(index);
    }

    /**
    * Sets the bits between from (inclusive) and to (exclusive) to true.
    *
    * @param from the start range (inclusive)
    * @param to the end range (exclusive)
    * @throws IndexOutOfBoundsException if from &lt; 0 || from &gt; to ||
    *         to &lt; 0
    * @since 1.4
*/
    public void set(int from, int to)
    {
        if (from < 0 || from > to)
            throw new ArgumentOutOfRangeException();
        if (from == to)
            return;
        int lo_offset = from >>> 6;
        int hi_offset = to >>> 6;
        ensure(hi_offset);
        if (lo_offset == hi_offset)
        {
            bits[hi_offset] |= (~0ul << from) & ((1uL << to) - 1);
            return;
        }

        bits[lo_offset] |= ~0ul << from;
        bits[hi_offset] |= (1uL << to) - 1;
        for (int i = lo_offset + 1; i < hi_offset; i++)
            bits[i] = ~0ul;
    }

    /**
    * Sets the bits between from (inclusive) and to (exclusive) to the
    * specified value.
    *
    * @param from the start range (inclusive)
    * @param to the end range (exclusive)
    * @param value the value to set it to
    * @throws IndexOutOfBoundsException if from &lt; 0 || from &gt; to ||
    *         to &lt; 0
    * @since 1.4
*/
    public void set(int from, int to, bool value)
    {
        if (value)
            set(from, to);
        else
            clear(from, to);
    }

    /**
    * Returns the number of bits actually used by this bit set.  Note
    * that this method doesn't return the number of set bits, and that
    * future requests for larger bits will make this automatically grow.
    *
    * @return the number of bits currently used.
*/
    public int size() => bits.Length * 64;

    /**
    * Returns the string representation of this bit set.  This
    * consists of a comma separated list of the integers in this set
    * surrounded by curly braces.  There is a space after each comma.
    * A sample string is thus "{1, 3, 53}".
    * @return the string representation.
*/
    public override string ToString()
    {
        StringBuilder r = new StringBuilder("{");
        bool first = true;
        for (int i = 0; i < bits.Length; ++i)
        {
            ulong bit = 1;
            ulong word = bits[i];
            if (word == 0)
                continue;
            for (int j = 0; j < 64; ++j)
            {
                if ((word & bit) != 0)
                {
                    if (!first)
                        r.Append(", ");
                    r.Append(64 * i + j);
                    first = false;
                }
                bit <<= 1;
            }
        }
        return r.Append("}").ToString();
    }

    /**
    * Performs the logical XOR operation on this bit set and the
    * given <code>set</code>.  This means it builds the symmetric
    * remainder of the two sets (the elements that are in one set,
    * but not in the other).  The result is stored into this bit set,
    * which grows as necessary.
    *
    * @param bs the second bit set
    * @throws NullPointerException if bs is null
*/
    public void xor(BitSet bs)
    {
        ensure(bs.bits.Length - 1);
        for (int i = bs.bits.Length - 1; i >= 0; i--)
            bits[i] ^= bs.bits[i];
    }

    /**
    * Make sure the vector is big enough.
    *
    * @param lastElt the size needed for the bits array
*/
    private void ensure(int lastElt)
    {
        if (lastElt >= bits.Length)
        {
            ulong[] nd = new ulong[lastElt + 1];
            Array.Copy(bits, 0, nd, 0, bits.Length);
            bits = nd;
        }
    }

    // This is used by EnumSet for efficiency.
    internal bool containsAll(BitSet other)
    {
        for (int i = other.bits.Length - 1; i >= 0; i--)
        {
            if ((bits[i] & other.bits[i]) != other.bits[i])
                return false;
        }
        return true;
    }
}