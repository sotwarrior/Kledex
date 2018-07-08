﻿using System;
using System.Threading.Tasks;
using OpenCqrs.Dependencies;
using OpenCqrs.Domain;
using OpenCqrs.Events;
using OpenCqrs.Exceptions;

namespace OpenCqrs.Commands
{
    /// <inheritdoc />
    /// <summary>
    /// CommandSenderAsync
    /// </summary>
    /// <seealso cref="T:OpenCqrs.Commands.ICommandSenderAsync" />
    public class CommandSenderAsync : ICommandSenderAsync
    {
        private readonly IResolver _resolver;
        private readonly IEventPublisherAsync _eventPublisherAsync;
        private readonly IEventFactory _eventFactory;
        private readonly IEventStore _eventStore;
        private readonly ICommandStore _commandStore;

        public CommandSenderAsync(IResolver resolver,
            IEventPublisherAsync eventPublisherAsync,
            IEventFactory eventFactory,
            IEventStore eventStore,
            ICommandStore commandStore)
        {
            _resolver = resolver;
            _eventPublisherAsync = eventPublisherAsync;
            _eventFactory = eventFactory;
            _eventStore = eventStore;
            _commandStore = commandStore;
        }

        /// <inheritdoc />
        public Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var handler = _resolver.Resolve<ICommandHandlerAsync<TCommand>>();

            if (handler == null)
                throw new HandlerNotFoundException(typeof(ICommandHandlerAsync<TCommand>));

            return handler.HandleAsync(command);
        }

        /// <inheritdoc />
        public async Task SendAsync<TCommand, TAggregate>(TCommand command) 
            where TCommand : IDomainCommand 
            where TAggregate : IAggregateRoot
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            await _commandStore.SaveCommandAsync<TAggregate>(command);

            var handler = _resolver.Resolve<ICommandHandlerWithDomainEventsAsync<TCommand>>();

            if (handler == null)
                throw new HandlerNotFoundException(typeof(ICommandHandlerWithDomainEventsAsync<TCommand>));

            var events = await handler.HandleAsync(command);

            foreach (var @event in events)
            {
                @event.CommandId = command.Id;
                var concreteEvent = _eventFactory.CreateConcreteEvent(@event);
                await _eventStore.SaveEventAsync<TAggregate>((IDomainEvent)concreteEvent);
            }
        }

        /// <inheritdoc />
        public async Task SendAndPublishAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var handler = _resolver.Resolve<ICommandHandlerWithEventsAsync<TCommand>>();

            if (handler == null)
                throw new HandlerNotFoundException(typeof(ICommandHandlerWithEventsAsync<TCommand>));

            var events = await handler.HandleAsync(command);

            foreach (var @event in events)
            {
                var concreteEvent = _eventFactory.CreateConcreteEvent(@event);
                await _eventPublisherAsync.PublishAsync(concreteEvent);
            }
        }

        /// <inheritdoc />
        public async Task SendAndPublishAsync<TCommand, TAggregate>(TCommand command) 
            where TCommand : IDomainCommand
            where TAggregate : IAggregateRoot
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            await _commandStore.SaveCommandAsync<TAggregate>(command);

            var handler = _resolver.Resolve<ICommandHandlerWithDomainEventsAsync<TCommand>>();

            if (handler == null)
                throw new HandlerNotFoundException(typeof(ICommandHandlerWithDomainEventsAsync<TCommand>));

            var events = await handler.HandleAsync(command);

            foreach (var @event in events)
            {
                @event.CommandId = command.Id;
                var concreteEvent = _eventFactory.CreateConcreteEvent(@event);
                await _eventStore.SaveEventAsync<TAggregate>((IDomainEvent)concreteEvent);
                await _eventPublisherAsync.PublishAsync(concreteEvent);
            }
        }
    }
}
