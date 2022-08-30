[<RequireQualifiedAccess>]
module Kicker.Domain.Utils

let find2D item (arr: 'T [,]) =
    let rec go x y =
        if y >= arr.GetLength 1 then
            None
        elif x >= arr.GetLength 0 then
            go 0 (y + 1)
        elif arr[x, y] = item then
            Some(x, y)
        else
            go (x + 1) y

    go 0 0

let spiral =
    let rec f (x, y) d m =
        seq {
            let mutable x = x
            let mutable y = y

            while 2 * x * d < m do
                yield x, y
                x <- x + d

            while 2 * y * d < m do
                yield x, y
                y <- y + d

            yield! f (x, y) -d (m + 1)
        }

    f (0, 0) 1 1

let isEven i = i % 2 = 0

let makeOdd i = if isEven i then i + 1 else i