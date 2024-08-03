using Elasticsearch.Net;
using Nest;
using News_aggregation.Entities;
using System;

namespace NewsAggregation.Services
{
    public class ElasticsearchClientFactory
    {
        private readonly IElasticClient _client;

        public ElasticsearchClientFactory()
        {
            var settings = new ConnectionSettings(new Uri("https://34.154.205.12:9200"))
            .DefaultIndex("articles")
                .BasicAuthentication("news", "cyKKAqHFvCk3F8p")

            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);


            _client = new ElasticClient(settings);

            EnsureIndexExists();
        }

        private void EnsureIndexExists()
        {
            if (_client == null)
            {
                throw new InvalidOperationException("ElasticClient is not initialized.");
            }

            var indexExistsResponse = _client.Indices.Exists("articles");

            if (!indexExistsResponse.Exists)
            {
                var createIndexResponse = _client.Indices.Create("articles", c => c
                    .Map<Article>(m => m
                        .Properties(p => p
                            .Keyword(k => k.Name(n => n.Id))
                            .Keyword(k => k.Name(n => n.AuthorId))
                            .Text(t => t.Name(n => n.Title))
                            .Text(t => t.Name(n => n.Content))
                            .Text(t => t.Name(n => n.Description))
                            .Keyword(k => k.Name(n => n.ImageUrl))
                            .Keyword(k => k.Name(n => n.SourceId))
                            .Keyword(k => k.Name(n => n.Url))
                            .Date(d => d.Name(n => n.PublishedAt))
                            .Date(d => d.Name(n => n.CreatedAt))
                            .Date(d => d.Name(n => n.UpdatedAt))
                            .Keyword(k => k.Name(n => n.CategoryId))
                            .Text(t => t.Name(n => n.Tags))
                            .Number(n => n.Name(n => n.Likes).Type(NumberType.Integer))
                            .Boolean(b => b.Name(n => n.IsPublished))
                            .Number(n => n.Name(n => n.Views).Type(NumberType.Integer))
                        )
                    )
                );

                if (!createIndexResponse.IsValid)
                {
                    throw new Exception("Failed to create index: " + createIndexResponse.ServerError.Error.Reason);
                }
            }
        }

        public IElasticClient GetClient()
        {
            return _client;
        }
    }
}
