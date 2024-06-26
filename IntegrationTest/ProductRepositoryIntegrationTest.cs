﻿using Core.Exceptions;
using Core.Models;
using DataAccess.Data;
using DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTest;

public class ProductRepositoryIntegrationTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryIntegrationTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        SeedAdmin();

        _repository = new ProductRepository(options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedAdmin()
    {
        IdentityUser defaultUser = new()
        {
            Id = "0206A018-5AC6-492D-AB99-10105193D384",
            Email = "admin@gmail.com",
            NormalizedEmail = "ADMIN@GMAIL.COM",
            UserName = "Admin",
            NormalizedUserName = "ADMIN",
            PasswordHash = new PasswordHasher<IdentityUser>().HashPassword(null!, "Password123!")
        };

        _context.Users.Add(defaultUser);
        _context.SaveChanges();
    }

    private void DetachEntities()
    {
        foreach (var entity in _context.ChangeTracker.Entries().ToList()) entity.State = EntityState.Detached;
    }

    [Fact]
    public void GetProductsBy_ReturnsProductsByUserId()
    {
        _context.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Product1",
                Price = 10.00M,
                Description = "Description1",
                UserId = "0206A018-5AC6-492D-AB99-10105193D384"
            },
            new Product
            {
                Id = 2,
                Name = "Product2",
                Price = 20.00M,
                Description = "Description2",
                UserId = "0206A018-5AC6-492D-AB99-10105193D384"
            },
            new Product
            {
                Id = 3,
                Name = "Product3",
                Price = 30.00M,
                Description = "Description3",
                UserId = "fakeId"
            }
        );
        _context.SaveChanges();

        var result = _repository.GetProductsBy("0206A018-5AC6-492D-AB99-10105193D384").ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal("0206A018-5AC6-492D-AB99-10105193D384", p.UserId));
    }

    [Fact]
    public void GetPageProducts_ReturnsPagedProducts()
    {
        _context.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Product1",
                Price = 10.00M,
                Description = "Description1",
                UserId = "UserId1"
            },
            new Product
            {
                Id = 2,
                Name = "Product2",
                Price = 20.00M,
                Description = "Description2",
                UserId = "UserId2"
            },
            new Product
            {
                Id = 3,
                Name = "Product3",
                Price = 30.00M,
                Description = "Description3",
                UserId = "UserId3"
            },
            new Product
            {
                Id = 4,
                Name = "Product4",
                Price = 40.00M,
                Description = "Description4",
                UserId = "UserId4"
            }
        );
        _context.SaveChanges();

        var result = _repository.GetPageProducts(1, 2).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Product2", result[0].Name);
        Assert.Equal("Product3", result[1].Name);
    }

    [Fact]
    public void GetProductCount_ReturnsProductCount()
    {
        _context.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Product1",
                Price = 10.00M,
                Description = "Description1",
                UserId = "UserId1"
            },
            new Product
            {
                Id = 2,
                Name = "Product2",
                Price = 20.00M,
                Description = "Description2",
                UserId = "UserId2"
            }
        );
        _context.SaveChanges();

        var count = _repository.GetProductCount();

        Assert.Equal(2, count);
    }

    [Fact]
    public void GetProductBy_ReturnsProduct()
    {
        _context.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Product1",
                Price = 10.00M,
                Description = "Description1",
                UserId = "UserId1"
            },
            new Product
            {
                Id = 2,
                Name = "Product2",
                Price = 20.00M,
                Description = "Description2",
                UserId = "UserId2"
            }
        );
        _context.SaveChanges();

        var result = _repository.GetProductBy(2);

        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Product2", result.Name);
    }

    [Fact]
    public void GetProductWithReviewsBy_ReturnsProduct()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Product1",
            Price = 10.00M,
            Description = "Description1",
            UserId = "0206A018-5AC6-492D-AB99-10105193D384"
        };
        _context.Products.Add(product);
        _context.Reviews.Add(new Review
        {
            Id = 1,
            ProductId = 1,
            Rating = 5,
            Comment = "Great!"
        });
        _context.SaveChanges();

        var result = _repository.GetProductWithReviewsBy(1);

        Assert.NotNull(result);
        Assert.NotNull(result.Reviews);
        Assert.Single(result.Reviews);
        Assert.Equal(5, result.Reviews.First().Rating);
        Assert.Equal("Great!", result.Reviews.First().Comment);
    }

    [Fact]
    public void CreateProduct_ReturnsId()
    {
        var product = new Product
        {
            Name = "Product1",
            Price = 10.00M,
            Description = "Description1",
            UserId = "UserId1"
        };

        var productId = _repository.CreateProduct(product);

        var createdProduct = _context.Products.Find(productId);
        Assert.NotNull(createdProduct);
        Assert.Equal(productId, createdProduct.Id);
    }

    [Fact]
    public void UpdateProduct_ReturnsTrue()
    {
        _context.Products.Add(new Product
        {
            Id = 1,
            Name = "Product1",
            Price = 11.0M,
            Description = "Base Description",
            UserId = "UserId1"
        });
        _context.SaveChanges();

        var updatedProduct = new Product
        {
            Name = "UpdatedProduct",
            Price = 10.0M,
            Description = "Updated Description",
            UserId = "UserId1"
        };

        var result = _repository.UpdateProduct(1, updatedProduct);
        DetachEntities();
        var product = _context.Products.FirstOrDefault(p => p.Id == 1);

        Assert.True(result);
        Assert.NotNull(product);
        Assert.Equal("UpdatedProduct", product.Name);
        Assert.Equal(10.0M, product.Price);
        Assert.Equal("Updated Description", product.Description);
    }

    [Fact]
    public void UpdateProduct_ThrowsNotFoundException()
    {
        _context.Products.Add(new Product
        {
            Id = 1,
            Name = "Product1",
            Price = 11.0M,
            Description = "Base Description",
            UserId = "UserId1"
        });
        _context.SaveChanges();

        var updatedProduct = new Product
        {
            Name = "UpdatedProduct",
            Price = 10.0M,
            Description = "Updated Description",
            UserId = "UserId1"
        };

        void UpdateProduct()
        {
            _repository.UpdateProduct(292347, updatedProduct);
        }

        Assert.Throws<NotFoundException>(UpdateProduct);
    }

    [Fact]
    public void DeleteProduct_ReturnsTrue()
    {
        _context.Products.Add(new Product
        {
            Id = 1,
            Name = "Product1",
            Price = 10.00M,
            Description = "Description1",
            UserId = "0206A018-5AC6-492D-AB99-10105193D384"
        });
        _context.SaveChanges();

        var result = _repository.DeleteProduct(1);

        Assert.True(result);
    }

    [Fact]
    public void DeleteProduct_ThrowsNotFoundException()
    {
        _context.Products.Add(new Product
        {
            Id = 1,
            Name = "Product1",
            Price = 10.00M,
            Description = "Description1",
            UserId = "0206A018-5AC6-492D-AB99-10105193D384"
        });
        _context.SaveChanges();

        void DeleteProduct()
        {
            _repository.DeleteProduct(21234);
        }

        Assert.Throws<NotFoundException>(DeleteProduct);
    }
}