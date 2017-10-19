using System;
using System.Collections.Generic;
using System.Web.Http;
using MissionMars.Models;

namespace MissionMars.Controllers
{
    public class TicketsController : ApiController
    {
        private static int _nextTicketId = 1;
        private static Dictionary<int, Ticket> _tickets = new Dictionary<int, Ticket>();

        [HttpPost]
        public IHttpActionResult Post(Ticket ticket)
        {
            int ticketId;

            Console.WriteLine(
                $"Ticket accepted: category:{ticket.Category} severity:{ticket.Severity} description:{ticket.Description}");

            lock (_tickets)
            {
                ticketId = _nextTicketId++;
                _tickets.Add(ticketId, ticket);
            }

            return Ok(ticketId.ToString());
        }
    }
}