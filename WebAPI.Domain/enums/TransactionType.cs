namespace WebAPI.Domain.enums;

public enum TransactionType
{
    TopUp = 1,      // Пополнение баланса
    Withdrawal = 2, // Списание средств
    Refund = 3,     // Возврат средств
    Payment = 4,    // Оплата услуг
    Bonus = 5       // Бонусные начисления
}
