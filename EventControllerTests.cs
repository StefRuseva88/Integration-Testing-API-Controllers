using Eventmi.Core.Models.Event;
using Eventmi.Infrastructure.Data.Contexts;
using Eventmi.Infrastructure.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using RestSharp;
using System.Net;
using static System.Net.WebRequestMethods;
namespace Eventmi.Tests
{
    public class Tests
    {
        private RestClient _client;
        private readonly string _baseUrl = "https://localhost:7236";

        [SetUp]
        public void Setup()
        {
             _client = new RestClient(_baseUrl);
        }

        [Test]
        public async Task GetAllEvents_ReturnSuccesStatusCode()
        {
            //Arrange
            var request = new RestRequest("/Event/All", Method.Get);
            //Act
            var response = await _client.ExecuteAsync(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task Add_GetRequest_ReturnsAddView()
        {
            //Arrange
            var request = new RestRequest("/Event/Add", Method.Get);
            //Act
            var response = await _client.ExecuteAsync(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void Add_PostRequest_AddsEventAndRedirects()
        {
            //Arrange
            var newEvent = new EventFormModel()
            {
                Name = "DEV: Challange Accepted",  
                Start = new DateTime(2024, 09, 29, 09, 0, 0),
                End = new DateTime(2024, 09, 29, 19, 0, 0),
                Place = "Sofia Tech Park"
            };
            var request = new RestRequest("/Event/Add", Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("Name", newEvent.Name);
            request.AddParameter("Start", newEvent.Start.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("End", newEvent.End.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("Place", newEvent.Place);
            

            //Act
            var response = _client.Execute(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(CheckEventExists(newEvent.Name), Is.True, "Event was not added to the database");
        }

        [Test]
        public async Task GetEventDetails_ReturnsSuccesAndExpectedContext()
        {
            //Arrange
            var eventId = 1;
            var request = new RestRequest($"/Event/Details/{eventId}", Method.Get);
            //Act
            var response = await _client.ExecuteAsync(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task Edit_GetRequest_ShouldReturnEditView()
        {
            //Arrange
            int? eventId = 1;
            var request = new RestRequest($"/Event/Details/{eventId}", Method.Get);
            //Act
            var response = await _client.ExecuteAsync(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task Edit_PostRequest_ShouldEditAnEvent()
        {
            //Arrange
            var eventId = 1;
            var dbEvent = GetEventById(eventId);

            var input = new EventFormModel()
            {
                Id = dbEvent.Id,
                Name = dbEvent.Name,
                Start = dbEvent.Start,
                End = dbEvent.End,
                Place = dbEvent.Place
            };

            var request = new RestRequest($"/Event/Edit/{dbEvent.Id}", Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("Id", dbEvent.Id);
            request.AddParameter("Name", dbEvent.Name);
            request.AddParameter("Start", dbEvent.Start.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("End", dbEvent.End.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("Place", dbEvent.Place);
            //Act
            var response = await _client.ExecuteAsync(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var updatedDbEvent = GetEventById(eventId);
            Assert.That(updatedDbEvent.Name, Is.EqualTo(input.Name));
        }

        [Test]
        public async Task Edit_WithIdMismatch_ShouldReturnEventNotFound()
        {
            // Arrange
            var eventId = 1;
            var dbEvent = GetEventById(eventId);

            var request = new RestRequest($"/Event/Edit/{eventId}", Method.Post);

            var input = new EventFormModel()
            {
                Id = 456, // Provide a non-existing ID
                Name = $"{dbEvent.Name} Updated!",
                Start = dbEvent.Start,
                End = dbEvent.End,
                Place = dbEvent.Place
            };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("Id", input.Id);
            request.AddParameter("Name", input.Name);
            request.AddParameter("Start", input.Start.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("End", input.End.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("Place", input.Place);

            // Act
            var response = await _client.ExecuteAsync(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task EditPostAction_WithInvalidModel_ShouldReturnView()
        {
            //Arrange
            int eventId = 1;
            var dbEvent = GetEventById(eventId);
            var request = new RestRequest($"/Event/Edit/{dbEvent.Id}", Method.Post);

            var input = new EventFormModel()
            {
                Id = dbEvent.Id,
                Name = dbEvent.Name,
            };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("Id", input.Id);
            request.AddParameter("Name", input.Name);
            
            //Act
            var response = await _client.ExecuteAsync(request);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        [Test]
        public async Task Delete_Action_WithValidIdShouldRedirectToAllEvents()
        {
            //Arrange
            var input = new EventFormModel()
            {
                Name = "Event for Deleting",
                Start = new DateTime(2024, 09, 29, 09, 0, 0),
                End = new DateTime(2024, 09, 29, 19, 0, 0),
                Place = "Sofia Tech Park"
            };
            var request = new RestRequest("/Event/Add", Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("Name", input.Name);
            request.AddParameter("Start", input.Start.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("End", input.End.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("Place", input.Place);

            await _client.ExecuteAsync(request);
            var eventInDb = GetEventByname(input.Name);
            var eventidToDelete = eventInDb.Id;

            var deleterequest = new RestRequest($"Event/Delete/{eventidToDelete}", Method.Post);
            //Act
            var response = await _client.ExecuteAsync(deleterequest);
            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        private bool CheckEventExists(string name)
        {
            var options = new DbContextOptionsBuilder<EventmiContext>
                ().UseSqlServer("Server=DESKTOP-T4LCS2N;Database=Eventmi;Trusted_Connection=True;MultipleActiveResultSets=True")
                .Options;
            using var context = new EventmiContext(options);
            return context.Events.Any(e => e.Name == name);
        }

        private Event GetEventById(int id)
        {
            var options = new DbContextOptionsBuilder<EventmiContext>
                ().UseSqlServer("Server=DESKTOP-T4LCS2N;Database=Eventmi;Trusted_Connection=True;MultipleActiveResultSets=True")
                .Options;
            using var context = new EventmiContext(options);
            return context.Events.FirstOrDefault(x => x.Id == id);
        }

        private Event GetEventByname(string name)
        {
            var options = new DbContextOptionsBuilder<EventmiContext>
                ().UseSqlServer("Server=DESKTOP-T4LCS2N;Database=Eventmi;Trusted_Connection=True;MultipleActiveResultSets=True")
                .Options;
            using var context = new EventmiContext(options);
            return context.Events.FirstOrDefault(e => e.Name == name);
        }
    }
}