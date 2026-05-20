// =====================================================================
//  14) MESSAGING & ASYNC PATTERNS — Interview Q&A
// =====================================================================
//  Covers: Queues vs Topics, brokers (Service Bus, RabbitMQ, Kafka),
//  delivery semantics, dead-letter, ordering, retries, idempotency,
//  patterns (pub/sub, work queue, request/reply, scheduled, fan-out).
// =====================================================================
namespace Interview.Messaging
{
    // ---------------------------------------------------------------------
    //  Q1: Why messaging instead of direct HTTP calls?
    //   - Decouple producer/consumer (different speeds, downtime tolerated).
    //   - Buffer load spikes.
    //   - Multiple consumers (fan-out).
    //   - Reliable async workflows (e.g., long-running jobs).
    // ---------------------------------------------------------------------

    // Q2: Queue vs Topic?
    // A : Queue - one message -> one consumer (work queue, load level).
    //     Topic - one message -> many subscribers (pub/sub).
    //     In Azure Service Bus: Queue + Topic primitives explicit.
    //     In RabbitMQ: it's all exchanges + queues with routing keys.
    //     In Kafka:  topic + consumer groups (each group gets all msgs).

    // Q3: Compare brokers quickly.
    // A : Azure Service Bus - enterprise, FIFO/sessions, dead-letter, sched.
    //     RabbitMQ          - flexible routing (direct, topic, fanout, header).
    //     Kafka             - high-throughput log; replay; partitioned ordering.
    //     Amazon SQS/SNS    - simple queue + pub/sub on AWS.
    //     Azure Event Hubs  - Kafka-like ingestion at huge scale.
    //     Azure Event Grid  - reactive, event routing (not durable storage).

    // ---------------------------------------------------------------------
    //  Q4: Delivery semantics
    // ---------------------------------------------------------------------
    // • At-most-once  : may lose, never duplicates.
    // • At-least-once : never lose, may duplicate -> need idempotent consumer.
    // • Exactly-once  : almost never truly free; achieved via outbox/inbox
    //                   (dedupe by message id) + transactional outbox.

    // Q5: Acks / autoack — what's the gotcha?
    // A : Auto-ack on receive = message is gone even if processing fails.
    //     Manual ack after success = safe but needs explicit Complete()
    //     or BasicAck(). On exception -> Abandon/Reject so it retries.

    // ---------------------------------------------------------------------
    //  Q6: Dead-letter queue (DLQ)?
    // ---------------------------------------------------------------------
    // After N failed deliveries or TTL expiry, the broker moves the message
    // to a DLQ for inspection. Build a tool/job to re-drive after fixes.
    // Always monitor DLQ depth.

    // Q7: Poison message?
    // A : A message that will fail forever (bad payload, missing dependency).
    //     Detect after N retries, route to DLQ, alert. Don't block the queue.

    // Q8: Ordering guarantees?
    // A : Service Bus  : FIFO per "session id".
    //     Kafka        : ordered within a partition (choose partition key).
    //     RabbitMQ     : ordered within a queue, but parallel consumers break order.
    //     SQS FIFO     : per "MessageGroupId".

    // Q9: Competing consumers?
    // A : Multiple consumers read the same queue; broker hands each message
    //     to ONE of them. Throughput scales by adding consumers (assuming
    //     the work is independent).

    // Q10: Pub/Sub fan-out?
    // A : Publish once to a topic -> N subscribers each receive a copy.
    //     Each subscriber has its own queue / log offset.

    // Q11: Request/Reply over messaging?
    // A : Reply-to queue + correlation id. Producer sends with these
    //     headers; consumer publishes reply to that reply-to. Use sparingly;
    //     HTTP/gRPC usually better for sync calls.

    // Q12: Scheduled / delayed messages?
    // A : Service Bus: ScheduledEnqueueTimeUtc.
    //     RabbitMQ: delayed-message plugin or dead-letter TTL trick.
    //     Useful for "send reminder in 24 hours" without a custom scheduler.

    // Q13: Outbox + Inbox (repeat from microservices file)
    // A : Outbox - publish reliably from a producer (atomic with DB).
    //     Inbox  - dedupe at the consumer (idempotency).

    // Q14: Backpressure / prefetch?
    // A : Limit how many messages a consumer pulls at once. Prevents OOM
    //     and lets the broker rebalance to a healthier consumer.
    //     RabbitMQ: basic.qos prefetchCount. Service Bus: prefetch count.

    // Q15: Idempotent consumer recipe.
    // A : Each message has a unique MessageId. On receive:
    //         BEGIN TX
    //           if EXISTS(SELECT 1 FROM ProcessedMessages WHERE Id=@id) RETURN;
    //           ... do work ...
    //           INSERT ProcessedMessages(Id, ProcessedAt) VALUES (@id, now());
    //         COMMIT TX

    // ---------------------------------------------------------------------
    //  AZURE SERVICE BUS (most common in .NET interviews)
    // ---------------------------------------------------------------------
    // • Namespaces, Queues, Topics+Subscriptions, Sessions, DLQ, sched msgs.
    // • SDK: Azure.Messaging.ServiceBus (new), .WithProcessor for high-level.
    //
    //   /// var client    = new ServiceBusClient(connStr);
    //   /// var processor = client.CreateProcessor("orders");
    //   /// processor.ProcessMessageAsync += async args => {
    //   ///     var body = args.Message.Body.ToString();
    //   ///     await Handle(body);
    //   ///     await args.CompleteMessageAsync(args.Message);
    //   /// };
    //   /// processor.ProcessErrorAsync += args => { log; return Task.CompletedTask; };
    //   /// await processor.StartProcessingAsync();

    // ---------------------------------------------------------------------
    //  KAFKA (when asked)
    // ---------------------------------------------------------------------
    // • Append-only log per partition. Consumer tracks OFFSET.
    // • Retention by time/size (replay possible).
    // • Throughput >> queues; ordering by partition key.
    // • Schema Registry + Avro/Protobuf for evolution.

    // ---------------------------------------------------------------------
    //  EVENT GRID vs EVENT HUBS vs SERVICE BUS
    // ---------------------------------------------------------------------
    // Service Bus - durable enterprise messaging (transactions, ordering, DLQ).
    // Event Hubs  - high-throughput INGESTION (telemetry, IoT, Kafka-compatible).
    // Event Grid  - reactive event ROUTING (cheap, broadcast, push w/ retries).

    // ---------------------------------------------------------------------
    //  SCENARIOS
    // ---------------------------------------------------------------------

    // [Scenario] Q16: After a deploy, the same email is sent 3 times to users.
    // A : Consumer not idempotent + at-least-once delivery. Add an Inbox
    //     table keyed on message id; only send if not processed.

    // [Scenario] Q17: A queue keeps growing — consumers can't keep up.
    // A : Scale out consumers (KEDA on Kubernetes), increase prefetch,
    //     batch processing, identify slow handler, add backpressure on producer.

    // [Scenario] Q18: Order messages must be processed in order PER customer
    //   but in parallel across customers.
    // A : Service Bus Sessions with SessionId = CustomerId. Each session is
    //     handled by one consumer at a time; different sessions in parallel.
    //     Kafka equivalent: partition key = CustomerId.

    // [Scenario] Q19: A producer commits to DB but the bus publish fails.
    // A : Outbox pattern: write business + outbox row in one tx; relay
    //     publishes outbox rows to the bus and marks them sent.

    // [Scenario] Q20: A consumer dies mid-processing — what happens?
    // A : Without ack, the broker redelivers to another consumer after the
    //     lock timeout. Make sure your handler is idempotent and the
    //     lock duration covers the worst-case processing time
    //     (or call RenewLock periodically).

    // [Scenario] Q21: How to migrate from RabbitMQ to Service Bus without
    //   downtime?
    // A : Dual-publish for a window; consumers read from both;
    //     once Service Bus catches up, switch producers off RabbitMQ.

    // [Scenario] Q22: Sensitive payload in messages — concerns?
    // A : Encrypt at rest (broker side) + in transit (TLS); avoid putting
    //     secrets in the body. Or send a pointer (e.g., blob URL) and
    //     resolve in the consumer using a managed identity.

    internal static class _Msg { }
}
