using System;
using MongoDB.Driver.Bson;

namespace MongoDB.Driver.Protocol
{
    /// <summary>
    ///   Description of QueryMessage.
    /// </summary>
    /// <remarks>
    ///   MsgHeader header;                 // standard message header
    ///   int32     opts;                   // query options.  See QueryOptions for values
    ///   cstring   fullCollectionName;     // "dbname.collectionname"
    ///   int32     numberToSkip;           // number of documents to skip when returning results
    ///   int32     numberToReturn;         // number of documents to return in the first OP_REPLY
    ///   BSON      query ;                 // query object.  See below for details.
    ///   [ BSON      returnFieldSelector; ]  // OPTIONAL : selector indicating the fields to return.  See below for details.
    /// </remarks>
    public class QueryMessage<T> : RequestMessageBase
        where T : class
    {
        public QueryMessage(){
            Header = new MessageHeader(OpCode.Query);
        }

        public QueryMessage(T query, String fullCollectionName)
            : this(query, fullCollectionName, 0, 0){
        }

        public QueryMessage(T query, String fullCollectionName, Int32 numberToReturn, Int32 numberToSkip)
            : this(query, fullCollectionName, numberToReturn, numberToSkip, null){
        }

        public QueryMessage(T query,
            String fullCollectionName,
            Int32 numberToReturn,
            Int32 numberToSkip,
            T returnFieldSelector){
            Header = new MessageHeader(OpCode.Query);
            Query = query;
            FullCollectionName = fullCollectionName;
            NumberToReturn = numberToReturn;
            NumberToSkip = numberToSkip;
            ReturnFieldSelector = returnFieldSelector;
        }

        public QueryOptions Options { get; set; }

        public string FullCollectionName { get; set; }

        public int NumberToSkip { get; set; }

        public int NumberToReturn { get; set; }

        public T Query { get; set; }

        public T ReturnFieldSelector { get; set; }

        protected override void WriteBody(BsonWriter writer){
            writer.WriteValue(BsonDataType.Integer, (int)Options);
            writer.Write(FullCollectionName, false);
            writer.WriteValue(BsonDataType.Integer, NumberToSkip);
            writer.WriteValue(BsonDataType.Integer, NumberToReturn);
            writer.WriteObject(Query);
            if(ReturnFieldSelector != null)
                writer.WriteObject(ReturnFieldSelector);
        }

        protected override int CalculateBodySize(BsonWriter writer){
            var size = 12; //options, numbertoskip, numbertoreturn
            size += writer.CalculateSize(FullCollectionName, false);
            size += writer.CalculateSizeObject(Query);
            if(ReturnFieldSelector != null)
                size += writer.CalculateSizeObject(ReturnFieldSelector);
            return size;
        }
    }
}