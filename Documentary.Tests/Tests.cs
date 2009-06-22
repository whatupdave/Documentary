using System.Data.SqlClient;
using NUnit.Framework;

namespace Documentary.Tests
{
    public class Post
    {
        public string Title { get; set; }
    }

    [TestFixture]
    public class Tests
    {
        private SqlConnectionStringBuilder _builder;
        private Schema _schema;

        [SetUp]
        public void SetUp()
        {
            _builder = new SqlConnectionStringBuilder
            {
                DataSource = @".\rowdy",
                InitialCatalog = @"_BlogDatabase",
                IntegratedSecurity = true
            };

            _schema = new Schema("_BlogDatabase", new SqlServerAdapter(_builder));
        }

        [Test]
        public void OnNewSchema_ShouldCreateDatabase()
        {
            using(var connection = new SqlConnection(_builder.ToString()))
            using(var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "select * from Documents";
                connection.Open();
                cmd.ExecuteReader();
            }
        }

        [Test]
        public void OnSavePost_ShouldSaveToDatabase()
        {
            _schema.Adapter.Table("Documents").Clear();

            _schema.Define<Post>(o => o.Title);

            using (var session = _schema.StartSession())
            {
                session.Save("/posts/1", new Post {Title = "sup!"});
            }

            var data = _schema.Adapter.Table("Documents").Select("Document").ToList();

            Assert.That(data[0]["Document"], Is.EqualTo("<Post><Title>sup!</Title></Post>"));
        }

        [Test]
        public void CanLoadDocumentBackOutOfTheDatabase()
        {
            _schema.Adapter.Table("Documents").Clear();

            _schema.Define<Post>(o => o.Title);

            using (var session = _schema.StartSession())
            {
                session.Save("/posts/1", new Post {Title = "sup!"});
            }

            using (var session = _schema.StartSession())
            {
                var post = session.Load<Post>("/posts/1");

                Assert.That(post.Title, Is.EqualTo("sup!"));
            }
        }

        [Test]
        public void CanQueryForObject()
        {
            _schema.Adapter.Table("Documents").Clear();
            _schema.Adapter.Table("Post").Clear();

            _schema.Define<Post>(o => o.Title);

            using (var session = _schema.StartSession())
            {
                session.Save("/posts/1", new Post {Title = "Post 1"});
                session.Save("/posts/2", new Post {Title = "Post 2"});
            }

            using (var session = _schema.StartSession())
            {
                var post = session.First<Post>("Title = 'Post 1'");

                Assert.That(post.Title, Is.EqualTo("Post 1"));
            }
        }
    }
}
