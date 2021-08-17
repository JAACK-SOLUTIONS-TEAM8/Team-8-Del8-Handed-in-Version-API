﻿using Louman.AppDbContexts;
using Louman.Models.DTOs.Order;
using Louman.Models.DTOs.Product;
using Louman.Models.Entities;
using Louman.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Louman.Repositories
{
    public class OrderRepository : IOrderRepository
    {


        private readonly AppDbContext _dbContext;

        public OrderRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<DeliveryTypeDto> AddDeliveryType(DeliveryTypeDto deliveryType)
        {
            if (deliveryType.DeliveryTypeId == 0)
            {
                var newDeliveryType = new DeliveryTypeEntity
                {
                    Description = deliveryType.Description,
                    isDeleted = false
                };
                _dbContext.DeliveryTypes.Add(newDeliveryType);
                await _dbContext.SaveChangesAsync();


                return await Task.FromResult(new DeliveryTypeDto
                {
                    DeliveryTypeId = newDeliveryType.DeliveryTypeId,
                    Description = deliveryType.Description
                });

            }
            else
            {

                var existingDeliveryType = await (from e in _dbContext.DeliveryTypes where e.DeliveryTypeId == deliveryType.DeliveryTypeId && e.isDeleted == false select e).SingleOrDefaultAsync();
                if (existingDeliveryType != null)
                {
                    existingDeliveryType.Description = deliveryType.Description;
                    _dbContext.Update(existingDeliveryType);
                    await _dbContext.SaveChangesAsync();

                    return await Task.FromResult(new DeliveryTypeDto
                    {
                        Description = existingDeliveryType.Description,
                        DeliveryTypeId = existingDeliveryType.DeliveryTypeId
                    });
                }
            }
            return new DeliveryTypeDto();

        }
        public async Task<bool> DeleteDeliveryType(int deliveryTypeId)
        {
            var deliveryType = _dbContext.DeliveryTypes.Find(deliveryTypeId);
            if (deliveryType != null)
            {
                deliveryType.isDeleted = true;
                _dbContext.DeliveryTypes.Update(deliveryType);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<List<DeliveryTypeDto>> GetAllDeliveryTypes()
        {
            return await (from d in _dbContext.DeliveryTypes
                          where d.isDeleted == false
                          orderby d.Description
                          select new DeliveryTypeDto
                          {
                              Description = d.Description,
                              DeliveryTypeId = d.DeliveryTypeId
                          }).ToListAsync();
        }
        public async Task<DeliveryTypeDto> GetDeliveryTypeById(int deloiveryTypeId)
        {
            return await (from d in _dbContext.DeliveryTypes
                          where d.isDeleted == false && d.DeliveryTypeId == deloiveryTypeId
                          orderby d.Description
                          select new DeliveryTypeDto
                          {
                              Description = d.Description,
                              DeliveryTypeId = d.DeliveryTypeId
                          }).SingleOrDefaultAsync();

        }
        public async Task<GetOrderDto> AddOrder(OrderDto order)
        {
            var newOrderEntity = new OrderEntity
            {
                ClientUserId = order.ClientUserId,
                DeliveryTypeId = order.DeliveryTypeId,
                PaymentType = order.PaymentType,
                OrderStatus = "Pending",
                PickupDate = DateTime.Parse(order.PickupDate),
                PickupTime = DateTime.Parse(order.PickupTime),
                CreatedDate = DateTime.Now,
                isDeleted = false
            };
            _dbContext.Orders.Add(newOrderEntity);
            await _dbContext.SaveChangesAsync();

            foreach (var product in order.Products)
            {
                var orderLineEntity = new OrderLineEntity
                {
                    OrderId = newOrderEntity.OrderId,
                    Quantity = product.Quantity,
                    ProductId = product.Product.ProductId,
                };
                await _dbContext.OrderLines.AddAsync(orderLineEntity);
                await _dbContext.SaveChangesAsync();

            }
            var billEntity = new OrderBillEntity
            {
                OrderId = newOrderEntity.OrderId,
                Total = order.Total,
                Discount = order.Discount
            };
            await _dbContext.OrderBills.AddAsync(billEntity);
            await _dbContext.SaveChangesAsync();

            var cardDetail = _dbContext.CardDetails.Where(card => card.ClientUserId == order.ClientUserId).SingleOrDefault();


            if (cardDetail == null)
            {
                var cardEntity = new CardDetailEntity
                {
                    HolderName = order.CardDetail.HolderName,
                    CardNumber = order.CardDetail.CardNumber,
                    ClientUserId = order.ClientUserId,
                    isDeleted = false,
                    SecurityNumber = order.CardDetail.SecurityNumber
                };
                await _dbContext.CardDetails.AddAsync(cardEntity);
                await _dbContext.SaveChangesAsync();

            }

              else
            {
                cardDetail.CardNumber = order.CardDetail.CardNumber;
                cardDetail.HolderName = order.CardDetail.HolderName;
                cardDetail.SecurityNumber = order.CardDetail.SecurityNumber;
                _dbContext.CardDetails.Update(cardDetail);
                await _dbContext.SaveChangesAsync();

            }


            return await Task.FromResult(
                    (from o in _dbContext.Orders
                     join u in _dbContext.Users on o.ClientUserId equals u.UserId
                     join dt in _dbContext.DeliveryTypes on o.DeliveryTypeId equals dt.DeliveryTypeId 
                     where o.ClientUserId == order.ClientUserId && o.OrderId == newOrderEntity.OrderId select           
                new GetOrderDto
                {
                    OrderId = newOrderEntity.OrderId,
                    BillId = billEntity.BillId,
                    ClientUserId = order.ClientUserId,
                    OrderStatus = newOrderEntity.OrderStatus,
                    Total = order.Total,
                    Discount = order.Discount,
                    DeliveryType =dt.Description,
                    CreatedDate=newOrderEntity.CreatedDate,
                    PaymentType=o.PaymentType,
                    PickupDate= o.PickupDate.Value.ToString("F"),
                    PickupTime=o.PickupTime.Value.ToString("F"),
                    ClientName = $"{u.Initials} {u.Surname}"
                }).SingleOrDefault());

            }


        public async Task<List<GetOrderDto>> GetAllClientOrders()
        {
            return
                   await (from o in _dbContext.Orders
                          join dt in _dbContext.DeliveryTypes on o.DeliveryTypeId equals dt.DeliveryTypeId
                          join b in _dbContext.OrderBills on o.OrderId equals b.OrderId
                          join u in _dbContext.Users on o.ClientUserId equals u.UserId
                          select new GetOrderDto
                          {
                              OrderId = o.OrderId,
                              BillId = b.BillId,
                              ClientUserId = o.ClientUserId,
                              OrderStatus = o.OrderStatus,
                              Total = b.Total.Value,
                              Discount = b.Discount.Value,
                              DeliveryType = dt.Description,
                              CreatedDate = o.CreatedDate,
                              PaymentType = o.PaymentType,
                              ClientName = $"{u.Initials} {u.Surname}",
                              PickupDate = o.PickupDate.Value.ToString("F"),
                              PickupTime = o.PickupTime.Value.ToString("F")
                          }).ToListAsync();
        }



    }
}

