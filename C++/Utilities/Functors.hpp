#ifndef FARB_FUNCTORS_HPP
#define FARB_FUNCTORS_HPP

#include <tuple>
#include <utility>

#include "ErrorOr.hpp"
#include "TypeInspection.hpp"
#include "BuiltinTypedefs.h"

namespace Farb
{

template<typename TRet, typename ...TArgs>
struct Functor
{
	virtual ~Functor() { };

	virtual TRet operator()(TArgs... args) = 0;

	virtual Functor * clone() const = 0;
};

template<typename ...TArgs>
struct Functor<void, TArgs...>
{
	virtual void operator()(TArgs... args) = 0;

	virtual Functor * clone() const = 0;

	virtual ~Functor() { };
};


template<typename TRet, typename ...TArgs>
struct FunctionPointer : public Functor<TRet, TArgs...>
{
	TRet (*func)(TArgs...);

	FunctionPointer(TRet (*func)(TArgs...))
		:func(func)
	{ }

	virtual TRet operator()(TArgs... args) override
	{
		return func(args...);
	}

	virtual Functor<TRet, TArgs...> * clone() const override
	{
		return new FunctionPointer(*this);
	}
};


// this lets you convert t.member(params) to func(t, params)
// what I'm really interested in is the other direction
// could use another operator (-> or | )
// see example at http://pfultz2.com/blog/2014/09/05/pipable-functions/
template <typename TRet, typename T, typename ...TArgs>
struct MemberFunction : public Functor<TRet, T&, TArgs...>
{
	TRet (T::*func)(TArgs...);

	MemberFunction(TRet (T::*func)(TArgs...))
		: func(func)
	{ }

	virtual TRet operator()(T& t, TArgs... args) override
	{
		return (t.*func)(args...);
	}

	virtual Functor<TRet, T&, TArgs...> * clone() const override
	{
		return new MemberFunction(*this);
	}
};


// Declaration first to support multiple parameter packs
// this doesn't need to have inheritence information
template<
	typename TRet,
	typename TypeListBefore,
	typename TArg,
	typename TypeListAfter>
struct CurriedFunctor;

// Specialization of template declaration with multiple parameter packs
template<typename TRet, typename ...TBefore, typename TArg, typename ...TAfter>
struct CurriedFunctor<
	TRet,
	TypeList<TBefore...>,
	TArg,
	TypeList<TAfter...> >
: public Functor<TRet, TBefore..., TAfter...>
{
	using TFunctor = Functor<TRet, TBefore..., TArg, TAfter...>;

	value_ptr<TFunctor> functor;
	TArg value;

	CurriedFunctor(value_ptr<TFunctor> && functor, TArg value)
		: functor(functor)
		, value(value)
	{ }

	virtual TRet operator()(TBefore... before, TAfter... after) override
	{
		return (*functor)(before..., value, after...);
	}

	virtual Functor<TRet, TBefore..., TAfter...> * clone() const override
	{
		return new CurriedFunctor(*this);
	}
};


// CurriedMember
/* new CurriedFunctor{
	new MemberFunction{
		func
	}, t
};
*/

template<typename TRet, typename T, typename ... TArgs>
CurriedFunctor<TRet, TypeList<>, T &, TypeList<TArgs...> > * MakeCurriedMember(
	TRet (T::*func)(TArgs...),
	T & t)
{
	MemberFunction< TRet, T, TArgs...> member { func };
	return new CurriedFunctor<TRet, TypeList<>, T&, TypeList<TArgs...> > {
		value_ptr<Functor<TRet, T&, TArgs...> > {
			new MemberFunction< TRet, T, TArgs...> { func }
		},
		t
	};
}

template<
	typename TRet,
	typename TListBefore,
	typename TArg,
	typename TListAfter,
	typename TList2Args> 
struct ComposedFunctors;

template<
	typename TRet,
	typename ...TBefore,
	typename TArg,
	typename ...TAfter,
	typename ...T2Args>
struct ComposedFunctors<
	TRet,
	TypeList<TBefore...>,
	TArg,
	TypeList<TAfter...>,
	TypeList<T2Args...> >
: public Functor <TRet, TBefore..., T2Args..., TAfter...>
{
	using TFunctor = Functor<TRet, TBefore..., typename UnwrapErrorOr<TArg>::TVal, TAfter...>;
	using TFunctorTwo = Functor<TArg, T2Args...>;

	value_ptr<TFunctor> functor;
	value_ptr<TFunctorTwo> functor_two;

	static_assert((IsErrorOr<TRet>::value && IsErrorOr<TArg>::value)
		|| !IsErrorOr<TArg>::value, "Composed functor_two returns an ErrorOr but the functor does not, therefore we don't know how to pass through the error. Maybe consider wrapping functor_two in a default lambda");

	// by convention assume that functions don't use ErrorOr as parameters
	ComposedFunctors(
		TFunctor & functor,
		TFunctorTwo & functor_two)
		: functor(&functor)
		, functor_two(&functor_two)
	{ }

	virtual TRet operator()(TBefore... before, T2Args... two_args, TAfter... after) override
	{
		if constexpr (IsErrorOr<TRet>::value && IsErrorOr<TArg>::value)
		{
			auto value = CHECK_RETURN((*functor_two)(two_args...));
			return (*functor)(before..., value, after...);
		}
		else
		{
			return (*functor)(before..., (*functor_two)(two_args...), after...);
		}
	}

	virtual Functor<TRet, TBefore..., T2Args..., TAfter...> * clone() const override
	{
		return new ComposedFunctors(*this);
	}
};

template<
	typename TRet,
	typename ...TArgs,
	typename TArg,
	typename ...T2Args>
inline auto Compose(
	Functor<TRet, TArgs...> & functor,
	Functor<TArg, T2Args...> & functor_two)
{
	using Split = SplitTypeList<
		typename UnwrapErrorOr<TArg>::TVal,
		TypeList<TArgs...> >;
	return ComposedFunctors<
		TRet,
		typename Split::Before,
		TArg,
		typename Split::After,
		TypeList<T2Args...> >(functor, functor_two);
}

// only removes a single copy, the second one
template<
	typename TRet,
	typename TDuplicate,
	typename TListBefore,
	typename TListBetween,
	typename TListAfter>
struct DuplicatedParamFunctor;

template<
	typename TRet,
	typename TDuplicate,
	typename... TBefore,
	typename... TBetween,
	typename... TAfter>
struct DuplicatedParamFunctor<
	TRet,
	TDuplicate,
	TypeList<TBefore...>,
	TypeList<TBetween...>,
	TypeList<TAfter...> >
: public Functor<TRet, TBefore..., TDuplicate, TBetween..., TAfter...>
{
	using TFunctor = Functor<
		TRet,
		TBefore...,
		TDuplicate,
		TBetween...,
		TDuplicate,
		TAfter...>;

	value_ptr<TFunctor> functor;

	DuplicatedParamFunctor(TFunctor & functor)
		: functor(&functor)
	{ }

	virtual TRet operator()(
		TBefore... before,
		TDuplicate duplicate,
		TBetween... between,
		TAfter... after) override
	{
		return (*functor)(
				before...,
				duplicate,
				between...,
				duplicate,
				after...);
	}

	virtual Functor<TRet, TBefore..., TDuplicate, TBetween..., TAfter...> * clone() const override
	{
		return new DuplicatedParamFunctor(*this);
	}
};

template<
	typename TDuplicate,
	typename TRet,
	typename... TArgs>
inline auto RemoveDuplicateParam(Functor<TRet, TArgs...> & functor)
{
	using SplitOne = SplitTypeList<TDuplicate, TypeList<TArgs...> >;
	using SplitTwo = SplitTypeList<TDuplicate, typename SplitOne::After>;

	return DuplicatedParamFunctor<
		TRet,
		TDuplicate,
		typename SplitOne::Before,
		typename SplitTwo::Before,
		typename SplitTwo::After>(functor);
}

// to be used to generate an AST where each function also takes a context
// it gets complicated with befores and afters, so lets assume the shared context
// is the first parameter. if it's not, call RemoveDuplicateParam yourself
template<
	typename TShared,
	typename TRet,
	typename... T1Args,
	typename TArg,
	typename... T2Args>
inline auto ComposeWithSharedParam(
	Functor<TRet, TShared, T1Args...> & f1,
	Functor<TArg, TShared, T2Args...> & f2)
{
	auto composed = Compose(f1, f2);
	return RemoveDuplicateParam<TShared>(composed);
}

template<typename TValue>
TValue Identity(TValue value)
{
	return value;
}

template<typename TValue>
auto IdentityFunctor(TValue value)
{
	return CurriedFunctor{FunctionPointer{Identity<TValue>}, value};
}

template<typename... TRemainder, typename TValue, typename... TRest>
Functor<TRemainder...> Curry(Functor<TRemainder..., TValue, TRest...> & functor, TValue value, TRest... rest)
{
	if constexpr (sizeof...(TRest) == 0 )
	{
		return CurriedFunctor{functor, value};
	}
	else
	{
		return Curry(CurriedFunctor{functor, value}, rest...);
	}
}


// rmf todo: how to specify partial specialization for containers?
// typename requires a fully resolved type
// can you template on a template name?
// maybe inference can just catch all this?
// type inference probably can't catch a return value...
template<template<typename, typename...> typename TContainer, typename TIn, typename TOut, typename ... TInArgs>
TContainer<TOut, TInArgs...> Map(
	const TContainer<TIn, TInArgs...>& in,
	Functor<TOut, const TIn &> & func)
{
	TContainer<TOut, TInArgs...> result{in.size()};
	for (const auto & val : in)
	{
		// rmf todo: which function to use here?
		// could we access by iterators in parallel?
		// could just pass in the function...
		result.push_back(func(val));
	}
}

template<template<typename, typename...> typename TContainer, typename TIn, typename ...TInArgs>
bool Apply(
	const TContainer<TIn, TInArgs...>& in,
	Functor<bool, TIn &> & apply,
	bool breakOnFailure = false)
{
	bool success = true;
	for (auto & val : in)
	{
		success = success && apply(val);
		if (breakOnFailure && !success)
		{
			return false;
		}
	}
	return success;
}

template<template<typename, typename...> typename TContainer, typename TIn, typename TOut, typename ... TInArgs>
TOut Reduce(
	const TContainer<TIn, TInArgs...> & in,
	Functor<TOut, const TOut &, const TIn &> & reduce,
	TOut initial = TOut{})
{
	TOut result = initial;
	for (const auto & val : in)
	{
		result = reduce(result, val);
	}
	return result;
}

template <typename TIn, typename TOut>
struct Sum : Functor<TOut, const TOut &, const TIn &>
{
	virtual TOut operator()(const TOut & aggregate, const TIn & nextValue) override
	{
		return aggregate + nextValue;
	}
};

template <typename T>
struct Max : Functor<T, const T &, const T &>
{
	virtual T operator()(const T & aggregate, const T & nextValue) override
	{
		if (nextValue > aggregate)
		{
			return nextValue;
		}
		return aggregate;
	}
};

template <typename T>
struct Min : Functor<T, const T &, const T &>
{
	virtual TOut operator()(const T & aggregate, const T & nextValue) override
	{
		if (nextValue < aggregate)
		{
			return nextValue;
		}
		return aggregate;
	}
};

} // namespace Farb

#endif // FARB_FUNCTORS_HPP