﻿using System;
using System.Threading.Tasks;
using Bolt.Cache.Dto;
using Bolt.Serializer;
using StackExchange.Redis;

namespace Bolt.Cache.Redis
{
    public class RedisCacheProvider : ICacheProvider
    {
        private readonly string _name;
        private readonly int _order;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ISerializer _serializer;
        private readonly IConnectionSettings _settings; 

        public RedisCacheProvider(string name, int order,
            IConnectionSettings settings,
            IConnectionFactory connectionFactory,
            ISerializer serializer)
        {
            _connectionFactory = connectionFactory;
            _serializer = serializer;
            _settings = settings;
            _name = name;
            _order = order;
        }

        private IDatabase Database
        {
            get { return _connectionFactory.Create().GetDatabase(_settings.Database); }
        }

        public bool Exists(string key)
        {
            return Database.KeyExists(key);
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Database.KeyExistsAsync(key);
        }

        public CacheResult<T> Get<T>(string key)
        {
            var result = Database.StringGet(key, CommandFlags.PreferSlave);

            return result.HasValue 
                ? new CacheResult<T>(true, result.IsNullOrEmpty ? default(T) : _serializer.Deserialize<T>(result)) 
                    : CacheResult<T>.Empty ;
        }

        public async Task<CacheResult<T>> GetAsync<T>(string key)
        {
            var result = await Database.StringGetAsync(key, CommandFlags.PreferSlave);

            return result.HasValue
                ? new CacheResult<T>(true, result.IsNullOrEmpty ? default(T) : _serializer.Deserialize<T>(result))
                    : CacheResult<T>.Empty;
        }

        public void Set<T>(string key, T value, int durationInSeconds)
        {
            if(value == null) return;

            Database.StringSet(key: key, 
                value: _serializer.Serialize(value), 
                expiry: TimeSpan.FromSeconds(durationInSeconds), 
                when: When.Always, 
                flags: CommandFlags.FireAndForget | CommandFlags.PreferMaster);
        }

        public Task SetAsync<T>(string key, T value, int durationInSeconds)
        {
            if (value == null) return Task.FromResult(0);

            return Database.StringSetAsync(key: key,
                value: _serializer.Serialize(value),
                expiry: TimeSpan.FromSeconds(durationInSeconds),
                when: When.Always,
                flags: CommandFlags.FireAndForget | CommandFlags.PreferMaster);
        }

        public void Remove(string key)
        {
            Database.KeyDelete(key, CommandFlags.FireAndForget | CommandFlags.PreferMaster);
        }

        public Task RemoveAsync(string key)
        {
            return Database.KeyDeleteAsync(key, CommandFlags.FireAndForget | CommandFlags.PreferMaster);
        }

        public int Order { get { return _order; } }
        public string Name { get { return _name; } }
    }
}
