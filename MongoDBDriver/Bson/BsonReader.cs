using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MongoDB.Driver.Bson
{
    /// <summary>
    /// Reads binary streams containing BSON data and converts them to native types.
    /// </summary>
    public class BsonReader
    {
        private Stream stream;
        private BinaryReader reader;        private int position = 0;

        private byte[] _byteBuffer;
        private char[] _charBuffer;

        private const int MaxCharBytesSize = 128;

        private byte[] seqRange1 = new byte[]{0,127};  //Range of 1-byte sequence
        private byte[] seqRange2 = new byte[]{194,223};//Range of 2-byte sequence
        private byte[] seqRange3 = new byte[]{224,239};//Range of 3-byte sequence
        private byte[] seqRange4 = new byte[]{240,244};//Range of 4-byte sequence
        
        private byte[] multiRange = new byte[]{128,191}; //Range of second, third, or fourth byte of a multi-byte sequence

        public BsonReader (Stream stream)
        {
            this.stream = stream;
            reader = new BinaryReader (this.stream);
        }

        public int Position {
            get { return position; }
        }

        public Document Read ()
        {
            position = 0;
            Document doc = ReadDocument();
            return doc;
        }

        public Document ReadDocument(){
            int startpos = position;
            Document doc = new Document ();
            int size = reader.ReadInt32 ();
            position += 4;
            while ((position - startpos) + 1 < size) {
                ReadElement (doc);
            }
            byte eoo = reader.ReadByte ();
            position++;
            if (eoo != 0)
                throw new InvalidDataException ("Document not null terminated");
            if (size != position - startpos) {
                throw new InvalidDataException (string.Format ("Should have read {0} bytes from stream but only read {1}", size, (position - startpos)));
            }
            return doc;
        }

        public void ReadElement (Document doc){
            sbyte typeNum = (sbyte)reader.ReadByte ();
            position++;
            String key = ReadString ();
            Object element = ReadElementType(typeNum);
            doc.Add (key, element);
        }

        public Object ReadElementType (sbyte typeNum){
            switch ((BsonDataType)typeNum) {
            case BsonDataType.Null:
            case BsonDataType.Undefined:
                return DBNull.Value;
            case BsonDataType.MinKey:
                return MongoMinKey.Value;
            case BsonDataType.MaxKey:
                return MongoMaxKey.Value;
            case BsonDataType.Boolean:
                position++;
                return reader.ReadBoolean ();
            case BsonDataType.Integer:
                position += 4;
                return reader.ReadInt32 ();
            case BsonDataType.Long:
                position += 8;
                return reader.ReadInt64 ();
            case BsonDataType.Date:
                position += 8;
                long millis = reader.ReadInt64 ();
                return BsonInfo.Epoch.AddMilliseconds(millis);
            case BsonDataType.Oid:
                position += 12;
                return new Oid (reader.ReadBytes (12));
            case BsonDataType.Number:
                position += 8;
                return reader.ReadDouble ();
            case BsonDataType.String:{
                return ReadLenString ();
            }
            case BsonDataType.Obj:{
                Document doc = this.ReadDocument();
                if(DBRef.IsDocumentDBRef(doc)){
                    return DBRef.FromDocument(doc);
                }
                return doc;
            }

            case BsonDataType.Array:{
                Document doc = this.ReadDocument();
                return ConvertToArray (doc);
            }
            case BsonDataType.Regex:{
                MongoRegex r = new MongoRegex ();
                r.Expression = this.ReadString ();
                r.Options = this.ReadString ();
                return r;
            }
            case BsonDataType.Code:{
                Code c = new Code ();
                c.Value = ReadLenString();
                return c;
            }
            case BsonDataType.CodeWScope:{
                int startpos = position;
                int size = reader.ReadInt32 ();
                position += 4;

                String val = this.ReadLenString();
                Document scope = this.ReadDocument();
                if (size != position - startpos) {
                    throw new System.IO.InvalidDataException (string.Format ("Should have read {0} bytes from stream but read {1} in CodeWScope", size, position - startpos));
                }
                return new CodeWScope (val, scope);
            }
            case BsonDataType.Binary:{
                int size = reader.ReadInt32 ();
                position += 4;
                byte subtype = reader.ReadByte ();
                position ++;
                if (subtype == (byte)Binary.TypeCode.General) {
                    size = reader.ReadInt32 ();
                    position += 4;
                }
                byte[] bytes = reader.ReadBytes (size);
                position += size;

                // From http://en.wikipedia.org/wiki/Universally_Unique_Identifier
                // The most widespread use of this standard is in Microsoft's Globally Unique Identifiers (GUIDs).
                if (subtype == 3 && 16 == size)
                {
                    return new Guid(bytes);
                }

                Binary b = new Binary();
                b.Bytes = bytes;
                b.Subtype = (Binary.TypeCode)subtype;
                return b;
            }
            default:
                throw new ArgumentOutOfRangeException (String.Format ("Type Number: {0} not recognized", typeNum));
            }
        }

        public string ReadString (){
            EnsureBuffers ();

            StringBuilder builder = new StringBuilder();
            int totalBytesRead = 0;
            int offset = 0;
            int times = 5;
            do {
                int byteCount = 0;
                int count = offset;
                byte b;
                while (count < MaxCharBytesSize && (b = reader.ReadByte ()) > 0) {
                    _byteBuffer[count++] = b;
                }
                byteCount = count - offset;
                totalBytesRead += byteCount;
                Console.WriteLine("read " + byteCount + " bytes. totalRead " + totalBytesRead );                
                position += byteCount;
                
                if(count == 0) break; //first byte read was the terminator.
                int lastFullCharStop = GetLastFullCharStop(count - 1);
                
                int charCount = Encoding.UTF8.GetChars (_byteBuffer, 0, lastFullCharStop + 1, _charBuffer, 0);
                builder.Append (_charBuffer, 0, charCount);

                if(lastFullCharStop < byteCount - 1){
                    offset = byteCount - lastFullCharStop - 1;
                    Console.WriteLine("New Offset is: " + offset);
                    //Copy end bytes to begining
                    Array.Copy(_byteBuffer, lastFullCharStop + 1, _byteBuffer, 0, offset);
                }else{
                    offset = 0;
                }
                
                if(b == 0){
                    Console.WriteLine("Read the null byte");
                    break;
                }else{
                    Console.WriteLine("Did not read the null byte");
                }
            } while (times-- > 0);
            //} while (true);
            position++;
            return builder.ToString();

        }

        /// <summary>
        /// Figures out how much if any bytes of a character may not have been completely read because it didn't
        /// fit into the byte buffer.
        /// </summary>
        /// <returns>
        /// The number of bytes in a incomplete character that have been read.
        /// </returns>
        private int DetermineRollBackAmount(int byteCount, byte b){
            int rollback = 0;
            //Check if we may have cut off a multibyte character.
            int seqSize = BytesInSequence(b);
            if(b > 0){
                if(seqSize > 1){ //last byte is the start so only character to rollback.
                    rollback = 1;
                }else if(IsMultiRangeByte(b)){
                    //look at the last few characters put in the buffer and figure out if it is a complete
                    //character.
                    rollback = 1;
                    for(int idx = byteCount - 2; idx > 0; idx--){
                        rollback++;
                        if(IsMultiByteStart(_byteBuffer[idx])){
                            if(BytesInSequence(_byteBuffer[idx]) == rollback){
                                //It read a full character
                                rollback = 0;
                            }
                            break;
                        }
                    }
                }
            }
            return rollback;
        }
        
        private bool IsMultiByteStart(byte b){
            return BytesInSequence(b) > 1;
        }
        
        private bool IsMultiRangeByte(byte b){
            if(b >= multiRange[0] && b <= multiRange[1]) return true;
            return false;
        }

        
        public string ReadLenString (){
            int length = reader.ReadInt32 ();
            string s = GetString (length - 1);
            reader.ReadByte ();

            position += (4 + 1);
            return s;
        }

        private string GetString (int length){
            if (length == 0)
                return string.Empty;

            EnsureBuffers ();

            StringBuilder builder = new StringBuilder (length);;
            
            Console.WriteLine("String " + length + " bytes.");
            int totalBytesRead = 0;
            int offset = 0;
            do {
                int byteCount = 0;                
                int count = ((length - totalBytesRead) > MaxCharBytesSize - offset) ? (MaxCharBytesSize - offset) :
                                                                            (length - totalBytesRead);
                
                Console.WriteLine("reading " + count + " bytes. Offset " + offset);
                byteCount = reader.BaseStream.Read (_byteBuffer, offset, count);
                totalBytesRead += byteCount;
                byteCount += offset;
                Console.WriteLine("read " + byteCount + " bytes. totalRead " + totalBytesRead );
                
                int lastFullCharStop = 0;
                if(byteCount > 1) lastFullCharStop = GetLastFullCharStop(byteCount - 1);
                
                if (byteCount == 0)
                    throw new EndOfStreamException ("Unable to read beyond the end of the stream.");

                position += byteCount;

                int charCount = Encoding.UTF8.GetChars (_byteBuffer, 0, lastFullCharStop + 1, _charBuffer, 0);
                builder.Append (_charBuffer, 0, charCount);
                
                if(lastFullCharStop < byteCount - 1){
                    offset = byteCount - lastFullCharStop - 1;
                    Console.WriteLine("New Offset is: " + offset);
                    //Copy end bytes to begining
                    Array.Copy(_byteBuffer, lastFullCharStop + 1, _byteBuffer, 0, offset);
                }else{
                    offset = 0;
                }
                
            } while (totalBytesRead < length);

            return builder.ToString ();
        }

        private int GetLastFullCharStop(int start){
            Console.WriteLine("starting at " + start);
            //if(start == 0) return 0;
            int bis = BytesInSequence(_byteBuffer[start]);
            if(bis == 1) return start;
            if(bis > 1) return start - 1; //start character is the begining so stop is one back.
            int back = 0;
            while((bis = BytesInSequence(_byteBuffer[start - back])) == 0){
                back++;
                Console.WriteLine("looking back " + back);
            }
            back = Math.Max(0, back - bis - 1);
            Console.WriteLine("back " + back);
            return start - back;
        }
        
        private int BytesInSequence(byte b){
            if(b <= seqRange1[1]) return 1;
            if(b >= seqRange2[0] && b <= seqRange2[1]) return 2;
            if(b >= seqRange3[0] && b <= seqRange3[1]) return 3;
            if(b >= seqRange4[0] && b <= seqRange4[1]) return 4;
            return 0;
        }
        
        private void EnsureBuffers (){
            if (_byteBuffer == null) {
                _byteBuffer = new byte[MaxCharBytesSize];
            }
            if (_charBuffer == null) {
                int charBufferSize = Encoding.UTF8.GetMaxCharCount (MaxCharBytesSize);
                _charBuffer = new char[charBufferSize];
            }
        }

        private Type GetTypeForIEnumerable (Document doc){
            if (doc.Keys.Count < 1)
                return typeof(Object);
            Type comp = null;
            foreach (String key in doc.Keys) {
                Object obj = doc[key];
                Type test = obj.GetType ();
                if (comp == null) {
                    comp = test;
                } else {
                    if (comp != test)
                        return typeof(Object);
                }
            }
            return comp;
        }

        private IEnumerable ConvertToArray (Document doc){
            var genericListType = typeof(List<>);
            var arrayType = GetTypeForIEnumerable(doc);
            var listType = genericListType.MakeGenericType(arrayType);

            var list = (IList)Activator.CreateInstance(listType);
            
            foreach (String key in doc.Keys) {
                list.Add(doc[key]);
            }

            return list;
        }
    }
}
