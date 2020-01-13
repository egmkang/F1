package util

import (
	"encoding/json"
	"github.com/pkg/errors"
	"github.com/unrolled/render"
	"io"
	"io/ioutil"
	"net/http"
	"time"
)

func ReadJSONResponseError(render *render.Render, w http.ResponseWriter, body io.ReadCloser, data interface{}) error {
	err := ReadJSON(body, data)
	if err == nil {
		return nil
	}

	render.JSON(w, http.StatusBadRequest, err.Error())
	return err
}

func ReadJSON(reader io.ReadCloser, data interface{}) error {
	defer reader.Close()

	bytes, err := ioutil.ReadAll(reader)
	if err != nil {
		return errors.WithStack(err)
	}

	err = json.Unmarshal(bytes, data)
	if err != nil {
		return errors.WithStack(err)
	}
	return nil
}

func JSON(data interface{}) (string, error) {
	bytes, err := json.Marshal(data)
	if err != nil {
		return "", errors.WithStack(err)
	}
	return string(bytes), nil
}

func GetMilliSeconds() int64 {
	now := time.Now()
	return now.UnixNano() / 1000 / 1000
}

func GetSeconds() int64 {
	now := time.Now()
	return now.Unix()
}
