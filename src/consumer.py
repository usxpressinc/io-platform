import asyncio
import logging
from asyncio import Task

from aiokafka import AIOKafkaConsumer
from aiokafka.helpers import create_ssl_context

from src.settings import Settings

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)
ssl_context = create_ssl_context()

consumer: AIOKafkaConsumer
consumer_task: Task[None]


async def kafka_consumer_start():
    global consumer, consumer_task
    consumer = AIOKafkaConsumer(
        settings.KAFKA__topic_rapid_alert,
        bootstrap_servers=settings.KAFKA__bootstrap_server.removeprefix(
            "SASL_SSL://"
        ),
        group_id=settings.KAFKA__consumer_group,
        security_protocol="SASL_SSL",
        sasl_mechanism="PLAIN",
        sasl_plain_password=settings.KAFKA__api_secret,
        sasl_plain_username=settings.KAFKA__api_key,
        client_id=settings.KAFKA__client_id,
        ssl_context=ssl_context,
        auto_offset_reset="latest",
    )
    # await consumer.start()
    consumer_task = asyncio.create_task(kafka_consume_messages())


async def kafka_consume_messages():
    global consumer
    try:
        # Consume messages
        async for msg in consumer:
            logger.info(
                "consumed: ",
                msg.topic,
                msg.partition,
                msg.offset,
                msg.key,
                msg.value,
                msg.timestamp,
            )
    except Exception as e:
        logger.error("Consumer loop exited: %s", repr(e))


async def kafka_consumer_end():
    global consumer, consumer_task
    if consumer_task:
        consumer_task.cancel()
        try:
            await consumer_task
        except asyncio.CancelledError:
            logger.info("Successfully cancelled consumer task")
    if consumer:
        await consumer.stop()
    logger.info("Successfully cancelled consumer")
