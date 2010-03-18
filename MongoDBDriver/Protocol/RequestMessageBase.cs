using System.IO;
using MongoDB.Driver.Bson;

namespace MongoDB.Driver.Protocol
{
    /// <summary>
    ///   Description of Message.
    /// </summary>
    public abstract class RequestMessageBase : MessageBase, IRequestMessage
    {
        private readonly IBsonObjectDescriptor _objectDescriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMessageBase"/> class.
        /// </summary>
        /// <param name="objectDescriptor">The object descriptor.</param>
        protected RequestMessageBase(IBsonObjectDescriptor objectDescriptor){
            _objectDescriptor = objectDescriptor;
        }

        /// <summary>
        /// Writes the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void Write(Stream stream){
            var header = Header;
            var bstream = new BufferedStream(stream);
            var writer = new BinaryWriter(bstream);
            var bwriter = new BsonWriter(bstream, _objectDescriptor);

            Header.MessageLength += CalculateBodySize(bwriter);
            if(Header.MessageLength > MaximumMessageSize)
                throw new MongoException("Maximum message length exceeded");
            writer.Write(header.MessageLength);
            writer.Write(header.RequestId);
            writer.Write(header.ResponseTo);
            writer.Write((int)header.OpCode);
            writer.Flush();
            WriteBody(bwriter);
            bwriter.Flush();
        }

        /// <summary>
        /// Writes the body.
        /// </summary>
        /// <param name="writer">The writer.</param>
        protected abstract void WriteBody(BsonWriter writer);

        /// <summary>
        /// Calculates the size of the body.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns></returns>
        protected abstract int CalculateBodySize(BsonWriter writer);
    }
}