﻿namespace MongoDB.Driver
{
    public class MongoMapReduceException : MongoCommandException
    {
        private MapReduce.MapReduceResult mrr;
        public MapReduce.MapReduceResult MapReduceResult{
            get{return mrr;}
        }
        
        public MongoMapReduceException(MongoCommandException mce, MapReduce mr):base(mce.Message,mce.Error, mce.Command){
            mrr = new MapReduce.MapReduceResult(mce.Error);
        }
    }
}