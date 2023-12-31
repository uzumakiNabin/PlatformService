﻿using AutoMapper;
using CommandsService.Data;
using CommandsService.DTOs;
using CommandsService.Models;
using System.Text.Json;

namespace CommandsService.EventProcessing
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;
        public EventProcessor(IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }
        public void ProcessEvent(string message)
        {
            EventType eventType = DetermineEvent(message);
            switch (eventType)
            {
                case EventType.PlatformPublished:
                    AddPlatform(message);
                    break;
                default: 
                    break;
            }
        }

        private EventType DetermineEvent(string notificationMessage)
        {
            Console.WriteLine("--> Determining event type");
            GenericEventDTO? eventType = JsonSerializer.Deserialize<GenericEventDTO>(notificationMessage);
            if (eventType != null)
            {
                switch (eventType.Event)
                {
                    case "Platform_Published":
                        Console.WriteLine("--> Platform published event detected");
                        return EventType.PlatformPublished;
                    default:
                        Console.WriteLine("--> Could not determine event type");
                        return EventType.Undetermined;
                }
            }
            else
            {
                return EventType.Undetermined;
            }
        }

        private void AddPlatform(string platformPublishedString)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ICommandRepo repo = scope.ServiceProvider.GetRequiredService<ICommandRepo>();

            PlatformPublishedDTO? platformPublished = JsonSerializer.Deserialize<PlatformPublishedDTO>(platformPublishedString);

            try
            {
                Platform platform = _mapper.Map<Platform>(platformPublished);
                if (!repo.ExternalPlatformExists(platform.ExternalId))
                {
                    repo.CreatePlatform(platform);
                    repo.SaveChanges();
                }
                else
                {
                    Console.WriteLine($"--> Platform already exists with external id {platform.ExternalId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not add Platform to db {ex.Message}");
            }
        }
    }

    enum EventType
    {
        PlatformPublished,
        Undetermined
    }
}
