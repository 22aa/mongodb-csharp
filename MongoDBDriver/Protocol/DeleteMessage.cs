/*
 * User: scorder
 */
using MongoDB.Driver.Bson;

namespace MongoDB.Driver.Protocol
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///   struct {
    ///   MsgHeader header;                 // standard message header
    ///   int32     ZERO;                   // 0 - reserved for future use
    ///   cstring   fullCollectionName;     // "dbname.collectionname"
    ///   int32     ZERO;                   // 0 - reserved for future use
    ///   BSON      selector;               // query object.  See below for details.
    ///   }
    /// </remarks>
    public class DeleteMessage : RequestMessageBase
    {
        public DeleteMessage(){
            Header = new MessageHeader(OpCode.Delete);
        }

        public string FullCollectionName { get; set; }

        public object Selector { get; set; }

        protected override void WriteBody(BsonWriter writer){
            writer.WriteValue(BsonDataType.Integer, 0);
            writer.Write(FullCollectionName, false);
            writer.WriteValue(BsonDataType.Integer, 0);
            writer.WriteObject(Selector);
        }

        protected override int CalculateBodySize(BsonWriter writer){
            var size = 8; //first int32, second int32 
            size += writer.CalculateSize(FullCollectionName, false);
            size += writer.CalculateSizeObject(Selector);
            return size;
        }
    }
}