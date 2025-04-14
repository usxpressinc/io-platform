# test_example.py
def add(x, y):
    return x + y


def test_add_positive_numbers():
    assert add(2, 3) == 5


def test_add_negative_numbers():
    assert add(-1, -1) == -2


def test_add_mixed_numbers():
    assert add(5, -2) == 3
