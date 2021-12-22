using System;
using UseAsmCode;

using static UseAsmCode.Invoker;

namespace ConsoleAppTestАssembler
{
    /// <summary>
    /// Класс для тестирования передачи\приёма 
    /// </summary>
    public class TestObject
    {
        /// <summary>
        /// Текст
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Поле А
        /// </summary>
        public int A;

        /// <summary>
        /// Свойство B
        /// </summary>
        public int B { get; set; }

        /// <summary>
        /// Сумма поля А и свойства B
        /// </summary>
        public int Sum { get { return A + B; } }
    }

    class Program
    {

        static readonly string _getAndReturnObject =
            @"
            mov eax, $first
            asmret
            ";

        static readonly string _asmInsertionSort =
            @"
            mov esi, $first
            mov edi, $second
            mov edi, [edi]
            mov ecx, 1
            cmp edi, 1
            jle L_end
            L_loop1:
            mov eax, [esi + ecx*4]
            mov edx, ecx
            L_loop2:
            mov ebx, [esi + edx*4 - 4]
            cmp eax, ebx
            jg L_loop2end
            mov [esi + edx*4], ebx
            dec edx
            jnz L_loop2
            L_loop2end:
            mov [esi + edx*4], eax
            inc ecx
            cmp ecx, edi
            jne L_loop1
            L_end:
            asmret
            ";

        static int[] _arr, _arr1, _arr2;

        static void Main(string[] args)
        {
            unsafe
            {
                Console.WriteLine("Вывод текста используя вставку на ассемблере");
                InvokeAsm((void*)0, (void*)0, new SASMCode(
                    @"
                    STD_OUTPUT_HANDLE equ -0Bh

                    extern GetStdHandle lib kernel32.dll
                    extern WriteConsoleW lib kernel32.dll
                
                    invoke GetStdHandle STD_OUTPUT_HANDLE
                    mov edx, addr V_OutHndl
                    mov [edx], eax
                    invoke WriteConsoleW eax, addr V_HelloMsg, 0Eh, addr V_WriteCount, 0
                    asmret

                    V_HelloMsg dw 'Hello World!', 0Dh, 0Ah, 0
                    V_OutHndl dd 0
                    V_WriteCount dd 0
                    ").Code);
            }
            Console.ReadLine();
            Console.Clear();

            #region Работа с передачей объекта
            Console.WriteLine("Передача собственного объекта:");
            TestObject testObject = new TestObject();
            Console.WriteLine("Введите текст:");
            testObject.Text = Console.ReadLine();
            Console.WriteLine($"Введите число №1 от От {int.MinValue} до {int.MaxValue}:");
            int.TryParse(Console.ReadLine(), out testObject.A);
            Console.WriteLine($"Введите число №2 от От {int.MinValue} до {int.MaxValue}:");
            testObject.B = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine($"Передаём объект типа {testObject.GetType()} с хеш-кодом {testObject.GetHashCode()}.");
            TestObject testObject2 = SafeInvokeAsm<TestObject, TestObject, TestObject>(ref testObject, ref testObject, new SASMCode(_getAndReturnObject));
            Console.WriteLine($"Получили объект типа {testObject2.GetType()} с хеш-кодом {testObject2.GetHashCode()}.");
            Console.WriteLine("Проверка функциональности:");
            Console.WriteLine($"Текст - {testObject2.Text}. А = {testObject2.A}, B = {testObject2.B}, Сумма = {testObject2.Sum}");
            Console.ReadLine();
            Console.Clear();
            #endregion

            #region Сравнение времени сортировки массива - ассемблер VS С#
            Console.WriteLine("Сравнение времени сортировки массива:");
            int arraySize = 10000;
            Console.Write($"Размер массива для сортировки = {arraySize}");
            //int arraySize = Convert.ToInt32(Console.ReadLine());
            _arr = new int[arraySize];
            _arr1 = new int[arraySize];
            _arr2 = new int[arraySize];
            Random rand = new Random();
            Console.WriteLine("Исходный массив:");
            for (int i = 0; i < arraySize; i++)
            {
                _arr[i] = rand.Next();
                Console.WriteLine($"{_arr[i]}");
            }
            Console.WriteLine("Исходный массив сформирован.\nНажмитие Enter для выполнения сортировки на ассемблере");
            Console.ReadLine();

            Array.Copy(_arr, _arr1, arraySize);
            Array.Copy(_arr, _arr2, arraySize);
            DateTime start1 = DateTime.Now;
            SASMCode sort = new SASMCode(_asmInsertionSort);
            DateTime start2 = DateTime.Now;
            unsafe
            {
                fixed (int* arrPtr = _arr1)
                {
                    InvokeAsm(arrPtr, &arraySize, (byte[])sort);
                }
            }
            DateTime finish = DateTime.Now;
            Console.WriteLine("Отсортированный массив на ассемблере:");
            foreach (int item in _arr1)
                Console.WriteLine($"{item}");
            
            var time1 = finish - start1;
            Console.WriteLine($"Сортировка вставками на ассемблере заняла: {time1}.");
            Console.WriteLine("Нажмитие Enter для выполнения сортировки на C#");
            Console.ReadLine();

            start1 = DateTime.Now;
            for (int i = 1; i < _arr2.Length; i++)
            {
                int tmp = _arr2[i];
                int j = i - 1;
                for (; j >= 0; j--)
                {
                    if (_arr2[j] > tmp) { _arr2[j + 1] = _arr2[j]; }
                    else
                    {
                        break;
                    }
                }
                _arr2[j + 1] = tmp;
            }
            finish = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine("Отсортированный массив на C#:");
            foreach (int item in _arr2)
                Console.WriteLine($"{item}");
            
            var time2 = finish - start1;
            Console.WriteLine($"Сортировка вставками на ассемблере заняла: {time1}.");
            Console.WriteLine($"Сортировка вставками на C# заняла: {time2}.");
            Console.WriteLine($"Разница: {time2 - time1}.");
            #endregion

            Console.WriteLine("Проверка расхождений...");
            Array.Sort(_arr);
            bool equal = true;
            for (int i = 0; i < arraySize; i++)
            {
                if (_arr[i] != _arr1[i] || _arr[i] != _arr2[i])
                {
                    Console.WriteLine($"Разные значения на позиции {i} (ожидаемое - ассемблер - C#): {_arr[i]} - {_arr1[i]} - {_arr2[i]}...");
                    equal = false;
                    break;
                }
            }
            Console.WriteLine((equal ? "Массивы отсортированы корректно!" : "Массивы отсортированы с ОШИБКАМИ!!!"));
            Console.ReadKey(true);
        }
    }
}
