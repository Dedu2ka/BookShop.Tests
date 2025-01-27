using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BookShop;
using System.Collections.Generic;
using Newtonsoft.Json;

public class HomeControllerTests
{
    private readonly Mock<ISession> _mockSession;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockSession = new Mock<ISession>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(ctx => ctx.Session).Returns(_mockSession.Object);

        _controller = new HomeController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            }
        };
    }

    [Fact]
    public void Cart_ReturnsViewWithCartItems()
    {
        // Arrange
        var cartItems = new List<CartItem>
    {
        new CartItem { Id = 1, Name = "Book 1", Price = 100, Quantity = 2 }
    };

        // ����������� ������ ������� � �������� ������
        var serializedCartItems = JsonConvert.SerializeObject(cartItems);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serializedCartItems);

        // ����������� ������ ��� ����������� ������ �������
        _mockSession.Setup(s => s.TryGetValue("Cart", out bytes)).Returns(true);

        // Act
        var result = _controller.Cart() as ViewResult;
        var model = result?.Model as List<CartItem>;

        // Assert
        Assert.NotNull(result); // ���������, ��� ��������� �� null
        Assert.NotNull(model);  // ���������, ��� ������ �������� � �������������
        Assert.Single(model);  // ����������, ��� � ������� ���� �������
        Assert.Equal(2, model[0].Quantity); // ��������� ���������� ������
    }


    [Fact]
    public void AddToCart_AddsNewItemToCart()
    {
        // Arrange
        var cartItems = new List<CartItem>();

        // ����������� ��������� ������ ������ � �������� ������
        var serializedCartItems = JsonConvert.SerializeObject(cartItems);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serializedCartItems);

        // ����������� ������, ����� ���������� ��������������� ������
        _mockSession.Setup(s => s.TryGetValue("Cart", out bytes)).Returns(true);

        byte[] updatedBytes = null;

        // ������������� ����� ������ Set ��� �������� ����������� ������
        _mockSession.Setup(s => s.Set("Cart", It.IsAny<byte[]>()))
                    .Callback<string, byte[]>((key, value) => updatedBytes = value);

        // Act
        _controller.AddToCart(1, "Book 1", 100);

        // Assert
        Assert.NotNull(updatedBytes);
        var updatedCartItems = JsonConvert.DeserializeObject<List<CartItem>>(System.Text.Encoding.UTF8.GetString(updatedBytes));
        Assert.Single(updatedCartItems);
        Assert.Equal(1, updatedCartItems[0].Id);
        Assert.Equal("Book 1", updatedCartItems[0].Name);
        Assert.Equal(100, updatedCartItems[0].Price);
        Assert.Equal(1, updatedCartItems[0].Quantity);

        // ���������, ��� Set ��� ������ ���� ���
        _mockSession.Verify(s => s.Set("Cart", It.IsAny<byte[]>()), Times.Once);
    }



    [Fact]
    public void RemoveFromCart_RemovesItem_WhenQuantityIsOne()
    {
        // Arrange
        var cartItems = new List<CartItem>
    {
        new CartItem { Id = 1, Name = "Book 1", Price = 100, Quantity = 1 }
    };

        // ����������� ������ ������� � �������� ������
        var serializedCartItems = JsonConvert.SerializeObject(cartItems);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serializedCartItems);

        // ����������� ���-������ ��� ����������� ������ �������
        _mockSession.Setup(s => s.TryGetValue("Cart", out bytes)).Returns(true);

        byte[] updatedBytes = null;

        // ������������� ����� ������ Set ��� �������� ����������� ������
        _mockSession.Setup(s => s.Set("Cart", It.IsAny<byte[]>()))
                    .Callback<string, byte[]>((key, value) => updatedBytes = value);

        // Act
        _controller.RemoveFromCart(1);

        // Assert
        Assert.NotNull(updatedBytes); // ��������, ��� ������ ���������
        var updatedCartItems = JsonConvert.DeserializeObject<List<CartItem>>(System.Text.Encoding.UTF8.GetString(updatedBytes));
        Assert.Empty(updatedCartItems); // ������� ������ ���� ������ �� �������

        // ���������, ��� Set ��� ������ ���� ���
        _mockSession.Verify(s => s.Set("Cart", It.IsAny<byte[]>()), Times.Once);
    }


    [Fact]
    public void RemoveFromCart_DecreasesQuantity_WhenQuantityGreaterThanOne()
    {
        // Arrange
        var cartItems = new List<CartItem>
    {
        new CartItem { Id = 1, Name = "Book 1", Price = 100, Quantity = 2 }
    };

        // ����������� ������ ������� � �������� ������
        var serializedCartItems = JsonConvert.SerializeObject(cartItems);
        var bytes = System.Text.Encoding.UTF8.GetBytes(serializedCartItems);

        // ����������� ���-������ ��� ����������� ������ �������
        _mockSession.Setup(s => s.TryGetValue("Cart", out bytes)).Returns(true);

        byte[] updatedBytes = null;

        // ������������� ����� ������ Set ��� �������� ����������� ������
        _mockSession.Setup(s => s.Set("Cart", It.IsAny<byte[]>()))
                    .Callback<string, byte[]>((key, value) => updatedBytes = value);

        // Act
        _controller.RemoveFromCart(1);

        // Assert
        Assert.NotNull(updatedBytes); // ��������, ��� ������ ���������
        var updatedCartItems = JsonConvert.DeserializeObject<List<CartItem>>(System.Text.Encoding.UTF8.GetString(updatedBytes));
        Assert.Single(updatedCartItems); // ��������, ��� ������� �������
        Assert.Equal(1, updatedCartItems[0].Quantity); // ���������� ������ ����������� �� 1

        // ���������, ��� Set ��� ������ ���� ���
        _mockSession.Verify(s => s.Set("Cart", It.IsAny<byte[]>()), Times.Once);
    }


    [Fact]
    public void PaymentSuccess_ClearsCart()
    {
        // Act
        var result = _controller.PaymentSuccess() as ViewResult;

        // Assert
        _mockSession.Verify(s => s.Remove("Cart"), Times.Once);
        Assert.NotNull(result);
    }
}
